using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;

namespace Metin2Bot.Infrastructure.Services
{
    public class WindowService : IWindowService
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public IEnumerable<WindowInfo> GetActiveWindows(string? titleFilter = null)
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, 256);
                string title = sb.ToString();

                if (string.IsNullOrWhiteSpace(title)) return true;

                if (!string.IsNullOrWhiteSpace(titleFilter)
                    && !title.Contains(titleFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                windows.Add(new WindowInfo { Handle = hWnd, Title = title });
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        public Rectangle GetWindowRect(IntPtr handle)
        {
            if (GetWindowRect(handle, out RECT rect))
            {
                return new Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            return Rectangle.Empty;
        }

        public IntPtr FindByTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return IntPtr.Zero;

            IntPtr found = IntPtr.Zero;
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, 256);
                string windowTitle = sb.ToString();

                if (windowTitle.Equals(title, StringComparison.Ordinal))
                {
                    found = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            return found;
        }

        public IEnumerable<WindowInfo> FindAllByTitle(string title)
        {
            var matches = new List<WindowInfo>();
            if (string.IsNullOrWhiteSpace(title)) return matches;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                StringBuilder sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, 256);
                string windowTitle = sb.ToString();

                if (windowTitle.Equals(title, StringComparison.Ordinal))
                {
                    matches.Add(new WindowInfo { Handle = hWnd, Title = windowTitle });
                }
                return true;
            }, IntPtr.Zero);

            return matches;
        }

        public bool IsMinimized(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return true;
            return IsIconic(handle);
        }

        public bool IsValidWindow(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return false;
            return IsWindow(handle);
        }

        public bool IsForeground(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return false;
            return GetForegroundWindow() == handle;
        }

        /// <summary>
        /// Pencereyi öne getir. Minimize ise restore eder. SetForegroundWindow Windows tarafından
        /// kısıtlanmıştır — başka bir thread foreground ise AttachThreadInput hilesi uygulanır.
        /// </summary>
        public bool BringToFront(IntPtr handle)
        {
            if (handle == IntPtr.Zero || !IsWindow(handle)) return false;

            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }
            else
            {
                ShowWindow(handle, SW_SHOW);
            }

            // Doğrudan dene
            if (SetForegroundWindow(handle)) return true;

            // Doğrudan başarısız olursa AttachThreadInput hilesi
            IntPtr fg = GetForegroundWindow();
            if (fg == handle) return true;

            uint targetThread = GetWindowThreadProcessId(handle, out _);
            uint fgThread = fg != IntPtr.Zero ? GetWindowThreadProcessId(fg, out _) : 0;
            uint thisThread = GetCurrentThreadId();

            bool attached1 = false, attached2 = false;
            try
            {
                if (fgThread != 0 && fgThread != thisThread)
                    attached1 = AttachThreadInput(thisThread, fgThread, true);
                if (targetThread != 0 && targetThread != thisThread)
                    attached2 = AttachThreadInput(thisThread, targetThread, true);

                BringWindowToTop(handle);
                bool ok = SetForegroundWindow(handle);
                return ok || GetForegroundWindow() == handle;
            }
            finally
            {
                if (attached1) AttachThreadInput(thisThread, fgThread, false);
                if (attached2) AttachThreadInput(thisThread, targetThread, false);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}

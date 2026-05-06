using System.Drawing;
using System.Runtime.InteropServices;

namespace Metin2Bot.Infrastructure.Services.Input
{
    internal sealed class NativeMouseInputDriver : IMouseInputDriver
    {
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MK_LBUTTON = 0x0001;

        public Point GetCursorPosition()
        {
            return GetCursorPos(out Point position) ? position : Point.Empty;
        }

        public void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public int MakeClientLParam(IntPtr handle, int screenX, int screenY)
        {
            Point clientPoint = new(screenX, screenY);
            ScreenToClient(handle, ref clientPoint);
            return ((clientPoint.Y & 0xFFFF) << 16) | (clientPoint.X & 0xFFFF);
        }

        public void PostLeftButtonDown(IntPtr handle, int lParam)
        {
            if (handle != IntPtr.Zero)
            {
                PostMessage(handle, WM_LBUTTONDOWN, MK_LBUTTON, lParam);
            }
        }

        public void PostLeftButtonUp(IntPtr handle, int lParam)
        {
            if (handle != IntPtr.Zero)
            {
                PostMessage(handle, WM_LBUTTONUP, 0, lParam);
            }
        }

        public void SendLeftButtonDown()
        {
            if (!SendMouseFlag(MOUSEEVENTF_LEFTDOWN))
            {
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            }
        }

        public void ForceLeftButtonUp(IntPtr handle, int lParam)
        {
            SendMouseFlag(MOUSEEVENTF_LEFTUP);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

            if (handle != IntPtr.Zero)
            {
                PostMessage(handle, WM_LBUTTONUP, 0, lParam);
            }
        }

        public bool IsForegroundWindow(IntPtr handle)
        {
            return handle != IntPtr.Zero && GetForegroundWindow() == handle;
        }

        IntPtr IMouseInputDriver.GetForegroundWindow()
        {
            return GetForegroundWindow();
        }

        private static bool SendMouseFlag(uint flag)
        {
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                u = new InputUnion
                {
                    mi = new MOUSEINPUT { dwFlags = flag }
                }
            };

            return SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>()) == 1;
        }
    }
}

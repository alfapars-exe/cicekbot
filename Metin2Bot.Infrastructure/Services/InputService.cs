using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using Metin2Bot.Application.Interfaces;

namespace Metin2Bot.Infrastructure.Services
{
    public class InputService : IInputService
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

        private readonly Random _random = new Random();
        
        // Çoklu ekranda (multi-client) farklı oyunların fareyi aynı anda kapışmasını engellemek için kilit
        private static readonly object _mouseLock = new object();

        public IntPtr FindWindow(string windowTitle) => IntPtr.Zero;

        public void BackgroundClick(IntPtr handle, int x, int y)
        {
            if (handle == IntPtr.Zero) return;

            lock (_mouseLock)
            {
                // Hedef offset ±8px (önce ±5) — daha geniş insan-benzeri sapma
                int targetX = x + _random.Next(-8, 9);
                int targetY = y + _random.Next(-8, 9);

                // Cursor'un mevcut pozisyonunu al, smooth move'un başlangıç noktası
                GetCursorPos(out Point origin);

                // Smooth ease-in-out hareket — anlık ışınlanma yerine doğal el hareketi.
                // Mesafeye orantılı süre (uzak hedef = daha uzun yol).
                int distance = (int)Math.Sqrt(Math.Pow(targetX - origin.X, 2) + Math.Pow(targetY - origin.Y, 2));
                int moveDuration = Math.Clamp(distance / 4 + _random.Next(60, 180), 80, 500);
                SmoothMove(origin.X, origin.Y, targetX, targetY, moveDuration);

                // Reaksiyon delay (insan görüş→tıklama latency'si)
                Thread.Sleep(_random.Next(60, 180));

                // Cursor target'ta mı doğrula
                GetCursorPos(out Point verify);
                if (Math.Abs(verify.X - targetX) > 3 || Math.Abs(verify.Y - targetY) > 3)
                {
                    SetCursorPos(targetX, targetY);
                    Thread.Sleep(_random.Next(20, 50));
                }

                // PostMessage backup için client koordinatları
                Point clientPoint = new Point(targetX, targetY);
                ScreenToClient(handle, ref clientPoint);
                int lParam = (clientPoint.Y << 16) | (clientPoint.X & 0xFFFF);

                var down = new INPUT { type = INPUT_MOUSE, u = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } } };
                var up = new INPUT { type = INPUT_MOUSE, u = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } } };

                // Hold süresi geniş aralık (200-550ms) — insan tıklama varyansını taklit
                int holdMs = _random.Next(200, 551);
                SendInput(1, new[] { down }, Marshal.SizeOf<INPUT>());
                Thread.Sleep(holdMs);
                SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
                PostMessage(handle, WM_LBUTTONUP, 0, lParam);

                // Insurance LEFTUP'ları
                int extraUps = _random.Next(2, 4);
                for (int i = 0; i < extraUps; i++)
                {
                    Thread.Sleep(_random.Next(25, 95));
                    SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
                    PostMessage(handle, WM_LBUTTONUP, 0, lParam);
                }

                // Click sonrası cursor target'ta kalır, micro-pause
                Thread.Sleep(_random.Next(50, 130));
            }
        }

        public void ForegroundClick(int screenX, int screenY)
        {
            lock (_mouseLock)
            {
                GetCursorPos(out Point originalPos);

                int targetX = screenX + _random.Next(-2, 3);
                int targetY = screenY + _random.Next(-2, 3);

                SetCursorPos(targetX, targetY);
                Thread.Sleep(20);

                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(_random.Next(40, 80));
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                Thread.Sleep(20);
                SetCursorPos(originalPos.X, originalPos.Y);
            }
        }

        public void HumanClick(int screenX, int screenY, int clickDurationMs)
        {
            lock (_mouseLock)
            {
                GetCursorPos(out Point origin);

                // Hedef etrafında küçük random sapma — aynı pikselde click bot fingerprint'idir
                int targetX = screenX + _random.Next(-3, 4);
                int targetY = screenY + _random.Next(-3, 4);

                // 1. Cursor'u hedefe smooth taşı (~80-150ms, ease-in-out)
                int moveDuration = _random.Next(80, 150);
                SmoothMove(origin.X, origin.Y, targetX, targetY, moveDuration);

                // 2. Tıklama öncesi reaksiyon delay'i (insan görüş→eylem latency'si)
                Thread.Sleep(_random.Next(40, 120));

                // 3. SendInput ile MOUSEDOWN — gerçek OS-level input
                SendMouseFlag(MOUSEEVENTF_LEFTDOWN);

                // 4. Click hold süresi (kullanıcı parametresi ± jitter)
                int hold = Math.Max(50, clickDurationMs + _random.Next(-50, 51));
                Thread.Sleep(hold);

                // 5. SendInput ile MOUSEUP
                SendMouseFlag(MOUSEEVENTF_LEFTUP);

                // 6. Tıklama sonrası kısa pause
                Thread.Sleep(_random.Next(30, 80));

                // 7. Cursor'u eski pozisyonuna smooth dön (kullanıcının fare hareketini bozmaz)
                SmoothMove(targetX, targetY, origin.X, origin.Y, _random.Next(60, 120));
            }
        }

        private static void SendMouseFlag(uint flag)
        {
            var input = new INPUT
            {
                type = INPUT_MOUSE,
                u = new InputUnion { mi = new MOUSEINPUT { dwFlags = flag } }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
        }

        /// <summary>
        /// Cursor'u (fromX,fromY)'den (toX,toY)'ye ease-in-out cubic ile yumuşak taşır.
        /// Anlık SetCursorPos ışınlanması yerine doğal hareket — bot tespitini zorlaştırır.
        /// </summary>
        private static void SmoothMove(int fromX, int fromY, int toX, int toY, int durationMs)
        {
            int distance = Math.Max(1, (int)Math.Sqrt(Math.Pow(toX - fromX, 2) + Math.Pow(toY - fromY, 2)));
            int steps = Math.Clamp(distance / 6, 8, 40);
            int sleepPerStep = Math.Max(2, durationMs / steps);

            for (int i = 1; i <= steps; i++)
            {
                double t = i / (double)steps;
                // Ease-in-out cubic — hızlanıp yavaşlama, doğal el hareketine benzer
                double eased = t < 0.5
                    ? 4 * t * t * t
                    : 1 - Math.Pow(-2 * t + 2, 3) / 2;

                int x = (int)Math.Round(fromX + (toX - fromX) * eased);
                int y = (int)Math.Round(fromY + (toY - fromY) * eased);
                SetCursorPos(x, y);
                if (i < steps) Thread.Sleep(sleepPerStep);
            }
        }

        public void BackgroundKeyPress(IntPtr handle, int keyCode)
        {
            // İleride eklenecek
        }
    }
}
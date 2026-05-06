using System.Drawing;
using System.Threading;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Infrastructure.Services.Input;

namespace Metin2Bot.Infrastructure.Services
{
    public class InputService : IInputService
    {
        private readonly IMouseInputDriver _mouse;

        // kbunall referans algoritması — kanıtlanmış çalışan değerler.
        // Cursor settle (teleport sonrası, click öncesi).
        private const int CursorSettleMs = 20;

        // Click hold (DOWN ile UP arası) — random aralık.
        private const int HoldMinMs = 30;
        private const int HoldMaxMs = 60;

        // Post-click bekleme (lock release öncesi).
        private const int PostClickMs = 10;

        // Coordinate jitter — küçük, ±2px (±20px değil; geniş jitter UI dışına taşma riski).
        private const int JitterAbs = 2;

        // Per-thread Random — multi-client'larda thread-affinity karışsa bile race olmaz,
        // sequence correlation'ı kırar.
        private static readonly ThreadLocal<Random> _rng = new(() =>
            new Random(unchecked(Environment.TickCount * 397 ^ Environment.CurrentManagedThreadId)));

        private static readonly object _mouseLock = new();

        public Action<string>? DiagnosticsLog { get; set; }

        public InputService()
            : this(new NativeMouseInputDriver())
        {
        }

        internal InputService(IMouseInputDriver mouse)
        {
            _mouse = mouse;
        }

        public IntPtr FindWindow(string windowTitle) => IntPtr.Zero;

        /// <summary>
        /// PostMessage tabanlı background click. kbunall referansının kanıtlanmış algoritması:
        /// cursor'u hedefe ışınla → 20ms settle → PostMessage WM_LBUTTONDOWN → 30-60ms hold →
        /// PostMessage WM_LBUTTONUP → cursor'u eski yerine geri koy.
        /// </summary>
        public void BackgroundClick(IntPtr handle, int x, int y)
        {
            if (handle == IntPtr.Zero) return;

            var rng = _rng.Value!;

            int targetX = x + rng.Next(-JitterAbs, JitterAbs + 1);
            int targetY = y + rng.Next(-JitterAbs, JitterAbs + 1);

            lock (_mouseLock)
            {
                Point originalPos = _mouse.GetCursorPosition();
                int lParam = _mouse.MakeClientLParam(handle, targetX, targetY);

                if (DiagnosticsLog is not null)
                {
                    IntPtr fg = _mouse.GetForegroundWindow();
                    Diag($"BackgroundClick: target=0x{handle.ToInt64():X} fg=0x{fg.ToInt64():X} screen=({targetX},{targetY}) lParam=0x{lParam:X8}");
                }

                // Cursor'u hedefe ışınla — oyun bazı durumlarda gerçek cursor'un da hedefte
                // olmasını ister (hover state). Click sonrası eski yere geri konuyor.
                _mouse.SetCursorPosition(targetX, targetY);
                Thread.Sleep(CursorSettleMs);

                _mouse.PostLeftButtonDown(handle, lParam);
                Thread.Sleep(rng.Next(HoldMinMs, HoldMaxMs + 1));
                _mouse.PostLeftButtonUp(handle, lParam);

                _mouse.SetCursorPosition(originalPos.X, originalPos.Y);
                Thread.Sleep(PostClickMs);
            }
        }

        public void ForegroundClick(int screenX, int screenY)
        {
            var rng = _rng.Value!;
            lock (_mouseLock)
            {
                Point originalPos = _mouse.GetCursorPosition();
                _mouse.SetCursorPosition(screenX, screenY);
                Thread.Sleep(CursorSettleMs);

                _mouse.SendLeftButtonDown();
                Thread.Sleep(rng.Next(HoldMinMs, HoldMaxMs + 1));
                _mouse.ForceLeftButtonUp(IntPtr.Zero, 0);

                _mouse.SetCursorPosition(originalPos.X, originalPos.Y);
            }
        }

        public void HumanClick(int screenX, int screenY, int clickDurationMs)
        {
            var rng = _rng.Value!;
            int hold = Math.Max(50, clickDurationMs + rng.Next(-30, 31));

            lock (_mouseLock)
            {
                Point originalPos = _mouse.GetCursorPosition();
                _mouse.SetCursorPosition(screenX, screenY);
                Thread.Sleep(CursorSettleMs);

                _mouse.SendLeftButtonDown();
                Thread.Sleep(hold);
                _mouse.ForceLeftButtonUp(IntPtr.Zero, 0);

                _mouse.SetCursorPosition(originalPos.X, originalPos.Y);
            }
        }

        public void ReleaseMouseButtons(IntPtr handle = default)
        {
            lock (_mouseLock)
            {
                int lParam = 0;
                if (handle != IntPtr.Zero)
                {
                    Point cursor = _mouse.GetCursorPosition();
                    lParam = _mouse.MakeClientLParam(handle, cursor.X, cursor.Y);
                }
                _mouse.ForceLeftButtonUp(handle, lParam);
                Thread.Sleep(PostClickMs);
            }
        }

        public void BackgroundKeyPress(IntPtr handle, int keyCode)
        {
            // Reserved for future use.
        }

        private void Diag(string message)
        {
            DiagnosticsLog?.Invoke(message);
        }
    }
}

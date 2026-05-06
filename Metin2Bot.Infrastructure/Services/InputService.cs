using System.Drawing;
using System.Threading;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Infrastructure.Services.Input;

namespace Metin2Bot.Infrastructure.Services
{
    public class InputService : IInputService
    {
        private readonly IMouseInputDriver _mouse;

        // Click timing — random aralıklar, sabit pattern'i kırar (anti-cheat)
        private const int CursorSettleMinMs = 50;
        private const int CursorSettleMaxMs = 90;
        private const int ClickHoldMinMs = 250;
        private const int ClickHoldMaxMs = 450;
        private const int PostClickWaitMinMs = 30;
        private const int PostClickWaitMaxMs = 80;
        private const int ReleaseSettleMs = 15;
        private const int TargetOffsetRange = 5; // ±5 px

        private static readonly Random _random = new();
        private static readonly object _mouseLock = new();

        public InputService()
            : this(new NativeMouseInputDriver())
        {
        }

        internal InputService(IMouseInputDriver mouse)
        {
            _mouse = mouse;
        }

        public IntPtr FindWindow(string windowTitle) => IntPtr.Zero;

        public void BackgroundClick(IntPtr handle, int x, int y)
        {
            if (handle == IntPtr.Zero) return;

            lock (_mouseLock)
            {
                // Hedef etrafında ±5px random offset — aynı pikselde click fingerprint'i engelle
                int targetX = x + _random.Next(-TargetOffsetRange, TargetOffsetRange + 1);
                int targetY = y + _random.Next(-TargetOffsetRange, TargetOffsetRange + 1);

                int lParam = _mouse.MakeClientLParam(handle, targetX, targetY);
                ReleaseBeforeClick(handle, lParam);

                _mouse.SetCursorPosition(targetX, targetY);
                Thread.Sleep(_random.Next(CursorSettleMinMs, CursorSettleMaxMs + 1));

                try
                {
                    _mouse.SendLeftButtonDown();
                    _mouse.PostLeftButtonDown(handle, lParam);

                    // Hold süresi her tıklamada random — anti-cheat pattern detection'ı kırar.
                    // 250-450ms insan tıklama varyansını taklit eder.
                    Thread.Sleep(_random.Next(ClickHoldMinMs, ClickHoldMaxMs + 1));
                }
                finally
                {
                    ReleaseAfterClick(handle, lParam);
                    Thread.Sleep(_random.Next(PostClickWaitMinMs, PostClickWaitMaxMs + 1));
                }
            }
        }

        public void ForegroundClick(int screenX, int screenY)
        {
            lock (_mouseLock)
            {
                Point originalPosition = _mouse.GetCursorPosition();
                ReleaseBeforeClick(IntPtr.Zero, 0);

                _mouse.SetCursorPosition(screenX, screenY);
                Thread.Sleep(_random.Next(CursorSettleMinMs, CursorSettleMaxMs + 1));

                try
                {
                    _mouse.SendLeftButtonDown();
                    Thread.Sleep(_random.Next(ClickHoldMinMs, ClickHoldMaxMs + 1));
                }
                finally
                {
                    ReleaseAfterClick(IntPtr.Zero, 0);
                    _mouse.SetCursorPosition(originalPosition.X, originalPosition.Y);
                }
            }
        }

        public void HumanClick(int screenX, int screenY, int clickDurationMs)
        {
            lock (_mouseLock)
            {
                Point originalPosition = _mouse.GetCursorPosition();
                ReleaseBeforeClick(IntPtr.Zero, 0);

                _mouse.SetCursorPosition(screenX, screenY);
                Thread.Sleep(_random.Next(CursorSettleMinMs, CursorSettleMaxMs + 1));

                try
                {
                    _mouse.SendLeftButtonDown();
                    Thread.Sleep(Math.Max(50, clickDurationMs + _random.Next(-30, 31)));
                }
                finally
                {
                    ReleaseAfterClick(IntPtr.Zero, 0);
                    _mouse.SetCursorPosition(originalPosition.X, originalPosition.Y);
                }
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
                Thread.Sleep(ReleaseSettleMs);
            }
        }

        public void BackgroundKeyPress(IntPtr handle, int keyCode)
        {
            // Reserved for future use.
        }

        private void ReleaseBeforeClick(IntPtr handle, int lParam)
        {
            _mouse.ForceLeftButtonUp(handle, lParam);
        }

        private void ReleaseAfterClick(IntPtr handle, int lParam)
        {
            _mouse.ForceLeftButtonUp(handle, lParam);
            Thread.Sleep(ReleaseSettleMs);
        }
    }
}

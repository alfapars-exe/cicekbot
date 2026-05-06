using System.Drawing;
using System.Threading;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Infrastructure.Services.Input;

namespace Metin2Bot.Infrastructure.Services
{
    public class InputService : IInputService
    {
        private readonly IMouseInputDriver _mouse;

        private const int CursorSettleMs = 20;
        private const int DefaultClickHoldMs = 60;
        private const int ReleaseSettleMs = 15;

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
                int lParam = _mouse.MakeClientLParam(handle, x, y);
                ReleaseBeforeClick(handle, lParam);

                _mouse.SetCursorPosition(x, y);
                Thread.Sleep(CursorSettleMs);

                try
                {
                    _mouse.SendLeftButtonDown();
                    _mouse.PostLeftButtonDown(handle, lParam);
                    Thread.Sleep(DefaultClickHoldMs);
                }
                finally
                {
                    ReleaseAfterClick(handle, lParam);
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
                Thread.Sleep(CursorSettleMs);

                try
                {
                    _mouse.SendLeftButtonDown();
                    Thread.Sleep(DefaultClickHoldMs);
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
                Thread.Sleep(CursorSettleMs);

                try
                {
                    _mouse.SendLeftButtonDown();
                    Thread.Sleep(Math.Max(50, clickDurationMs));
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

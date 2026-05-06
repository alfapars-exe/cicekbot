using Metin2Bot.Application.Interfaces;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Metin2Bot.UI.Services
{
    /// <summary>
    /// Win32 RegisterHotKey ile DEL tuşunu global hotkey olarak kaydeder.
    /// HwndSource hook'u WndProc içinde WM_HOTKEY mesajını yakalar.
    /// </summary>
    public class HotkeyService : IHotkeyService
    {
        private const int HOTKEY_ID = 0xB0B; // arbitrary unique id
        private const uint MOD_NONE = 0x0000;
        private const uint VK_DELETE = 0x2E;
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource? _source;
        private IntPtr _hwnd;
        private Action? _onPressed;
        private bool _registered;

        public bool Register(IntPtr windowHandle, Action onPressed)
        {
            if (_registered) Unregister();

            _hwnd = windowHandle;
            _onPressed = onPressed;
            _source = HwndSource.FromHwnd(windowHandle);
            _source?.AddHook(WndProc);

            _registered = RegisterHotKey(_hwnd, HOTKEY_ID, MOD_NONE, VK_DELETE);
            return _registered;
        }

        public void Unregister()
        {
            if (_registered && _hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID);
            }
            _source?.RemoveHook(WndProc);
            _source = null;
            _onPressed = null;
            _registered = false;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                _onPressed?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose() => Unregister();
    }
}

using System;

namespace Metin2Bot.Application.Interfaces
{
    public interface IInputService
    {
        IntPtr FindWindow(string windowTitle);

        /// <summary>
        /// PostMessage tabanlı background click. Pencere arka planda olsa bile çalışır;
        /// foreground gerektirmez.
        /// </summary>
        void BackgroundClick(IntPtr handle, int x, int y);

        /// <summary>
        /// Foreground gerçek mouse tıklaması (mouse_event/SendInput). Pencerenin önde olduğu varsayılır.
        /// </summary>
        void ForegroundClick(int screenX, int screenY);

        /// <summary>
        /// İnsan benzeri tıklama: smooth cursor hareketi + parametrik click hold süresi.
        /// Pencerenin foreground olduğu varsayılır.
        /// </summary>
        void HumanClick(int screenX, int screenY, int clickDurationMs);

        /// <summary>
        /// Mouse button'larını zorla release eder. Önceki bir tıklamadan kalan basılı state'i sıfırlar.
        /// </summary>
        void ReleaseMouseButtons(IntPtr handle = default);

        void BackgroundKeyPress(IntPtr handle, int keyCode);

        /// <summary>
        /// Diagnostic log callback. BotEngine her tıklamanın detaylarını (target HWND, foreground HWND,
        /// lParam) buraya akıtır. Null bırakılırsa logging yok.
        /// </summary>
        Action<string>? DiagnosticsLog { get; set; }
    }
}

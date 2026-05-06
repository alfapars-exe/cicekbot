using System;

namespace Metin2Bot.Application.Interfaces
{
    public interface IInputService
    {
        IntPtr FindWindow(string windowTitle);

        /// <summary>
        /// Cursor teleport + mouse_event tabanlı tıklama (orijinal repo davranışı).
        /// </summary>
        void BackgroundClick(IntPtr handle, int x, int y);

        /// <summary>
        /// Foreground gerçek mouse tıklaması (mouse_event). Pencerenin önde olduğu varsayılır.
        /// </summary>
        void ForegroundClick(int screenX, int screenY);

        /// <summary>
        /// İnsan benzeri tıklama: smooth cursor hareketi + SendInput ile gerçek mouse input
        /// + parametrik click hold süresi. Pencerenin foreground olduğu varsayılır.
        /// </summary>
        /// <param name="screenX">Hedef X (ekran koordinatı)</param>
        /// <param name="screenY">Hedef Y (ekran koordinatı)</param>
        /// <param name="clickDurationMs">Click basılı tutma süresi (ms). 300-500 önerilir.</param>
        void HumanClick(int screenX, int screenY, int clickDurationMs);

        /// <summary>
        /// Mouse button'larını zorla release eder. Önceki bir tıklamadan kalan
        /// "basılı" state'i sıfırlamak için kullanılır (özellikle client switch'ten önce).
        /// </summary>
        /// <param name="handle">Hedef pencere handle'ı; varsa pencereye PostMessage WM_LBUTTONUP da gönderilir. Zero geçilirse sadece global SendInput LEFTUP.</param>
        void ReleaseMouseButtons(IntPtr handle = default);

        void BackgroundKeyPress(IntPtr handle, int keyCode);
    }
}

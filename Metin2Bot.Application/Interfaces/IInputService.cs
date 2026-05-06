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
        /// + parametrik click hold süresi + jitter. Pencerenin foreground olduğu varsayılır.
        /// </summary>
        /// <param name="screenX">Hedef X (ekran koordinatı)</param>
        /// <param name="screenY">Hedef Y (ekran koordinatı)</param>
        /// <param name="clickDurationMs">Click basılı tutma süresi (ms). 300-500 önerilir.</param>
        void HumanClick(int screenX, int screenY, int clickDurationMs);

        void BackgroundKeyPress(IntPtr handle, int keyCode);
    }
}

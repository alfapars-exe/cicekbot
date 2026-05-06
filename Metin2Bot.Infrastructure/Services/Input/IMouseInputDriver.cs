using System.Drawing;

namespace Metin2Bot.Infrastructure.Services.Input
{
    internal interface IMouseInputDriver
    {
        Point GetCursorPosition();
        void SetCursorPosition(int x, int y);
        int MakeClientLParam(IntPtr handle, int screenX, int screenY);

        // PostMessage tabanlı (background) — varsayılan tıklama yolu.
        void PostLeftButtonDown(IntPtr handle, int lParam);
        void PostLeftButtonUp(IntPtr handle, int lParam);

        // SendInput / mouse_event tabanlı (foreground) — ForegroundClick / HumanClick için.
        void SendLeftButtonDown();
        void ForceLeftButtonUp(IntPtr handle, int lParam);

        // Diagnostics.
        bool IsForegroundWindow(IntPtr handle);
        IntPtr GetForegroundWindow();
    }
}

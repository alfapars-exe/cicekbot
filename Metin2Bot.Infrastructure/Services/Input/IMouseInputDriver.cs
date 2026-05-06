using System.Drawing;

namespace Metin2Bot.Infrastructure.Services.Input
{
    internal interface IMouseInputDriver
    {
        Point GetCursorPosition();
        void SetCursorPosition(int x, int y);
        int MakeClientLParam(IntPtr handle, int screenX, int screenY);
        void SendLeftButtonDown();
        void ForceLeftButtonUp(IntPtr handle, int lParam);
        void PostLeftButtonDown(IntPtr handle, int lParam);
    }
}

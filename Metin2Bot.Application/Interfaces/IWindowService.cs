using Metin2Bot.Domain.Models;
using System.Collections.Generic;

namespace Metin2Bot.Application.Interfaces
{
    public interface IWindowService
    {
        IEnumerable<WindowInfo> GetActiveWindows(string? titleFilter = null);
        System.Drawing.Rectangle GetWindowRect(IntPtr handle);
        IntPtr FindByTitle(string title);
        IEnumerable<WindowInfo> FindAllByTitle(string title);
        bool IsMinimized(IntPtr handle);
        bool IsValidWindow(IntPtr handle);
        bool BringToFront(IntPtr handle);
        bool IsForeground(IntPtr handle);
    }
}

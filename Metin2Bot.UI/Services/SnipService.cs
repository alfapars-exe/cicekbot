using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using Metin2Bot.UI.Views;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;

namespace Metin2Bot.UI.Services
{
    /// <summary>
    /// Snip servisi — kullanıcının seçili client penceresinde dikdörtgen çizip
    /// o alanı PNG olarak kaydetmesini sağlar.
    /// UI katmanında çünkü WPF Window'a bağımlı.
    /// </summary>
    [SupportedOSPlatform("windows6.1")]
    public class SnipService : ISnipService
    {
        private readonly IWindowService _windowService;
        private readonly IConfigStore _configStore;

        public SnipService(IWindowService windowService, IConfigStore configStore)
        {
            _windowService = windowService;
            _configStore = configStore;
        }

        public ProductTemplate? SnipFromWindow(IntPtr windowHandle, Guid clientId, string suggestedName)
        {
            if (windowHandle == IntPtr.Zero) return null;

            var clientRect = _windowService.GetWindowRect(windowHandle);
            if (clientRect.Width <= 0 || clientRect.Height <= 0) return null;
            if (_windowService.IsMinimized(windowHandle)) return null;

            var overlay = new SnipOverlay();
            // Ekran koordinatlarını DIP'e çevir (DPI-aware)
            var dipRect = ScreenRectToDip(clientRect);
            overlay.Left = dipRect.X;
            overlay.Top = dipRect.Y;
            overlay.Width = dipRect.Width;
            overlay.Height = dipRect.Height;

            bool? ok = overlay.ShowDialog();
            if (ok != true || overlay.SelectedRect is null) return null;

            // overlay'in seçim rect'i overlay'in client koordinatları (DIP)
            // Bunu ekran koordinatına çevir
            var selectionDip = overlay.SelectedRect.Value;
            var screenSelection = DipRectToScreen(
                new Rect(dipRect.X + selectionDip.X, dipRect.Y + selectionDip.Y,
                         selectionDip.Width, selectionDip.Height));

            int sx = (int)Math.Round(screenSelection.X);
            int sy = (int)Math.Round(screenSelection.Y);
            int sw = (int)Math.Round(screenSelection.Width);
            int sh = (int)Math.Round(screenSelection.Height);

            if (sw < 4 || sh < 4) return null;

            // Ekrandan kırp
            using var bmp = new Bitmap(sw, sh, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(sx, sy, 0, 0, new System.Drawing.Size(sw, sh), CopyPixelOperation.SourceCopy);
            }

            // PNG kaydet
            var newId = Guid.NewGuid();
            var dir = _configStore.GetClientTemplatesDirectory(clientId);
            var path = Path.Combine(dir, $"{newId}.png");
            bmp.Save(path, ImageFormat.Png);

            return new ProductTemplate
            {
                Id = newId,
                Name = suggestedName,
                ImagePath = path,
                SourceRegion = new Metin2Bot.Domain.Models.Region(sx, sy, sw, sh)
            };
        }

        // --- DPI helpers ---
        private static Rect ScreenRectToDip(Rectangle screenRect)
        {
            var (sx, sy) = GetDpiScale();
            return new Rect(screenRect.X / sx, screenRect.Y / sy,
                            screenRect.Width / sx, screenRect.Height / sy);
        }

        private static Rect DipRectToScreen(Rect dipRect)
        {
            var (sx, sy) = GetDpiScale();
            return new Rect(dipRect.X * sx, dipRect.Y * sy,
                            dipRect.Width * sx, dipRect.Height * sy);
        }

        private static (double sx, double sy) GetDpiScale()
        {
            // System DPI'yı bitmap üzerinden oku — desktop HWND gerektirmiyor.
            // Multi-monitor per-monitor DPI desteği için ileride PresentationSource.FromVisual
            // (mevcut MainWindow) kullanılabilir; şimdilik system-wide DPI yeterli.
            try
            {
                using var bmp = new Bitmap(1, 1);
                using var g = Graphics.FromImage(bmp);
                return (g.DpiX / 96.0, g.DpiY / 96.0);
            }
            catch
            {
                return (1.0, 1.0);
            }
        }
    }
}

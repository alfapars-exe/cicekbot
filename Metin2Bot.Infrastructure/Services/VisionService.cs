using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Metin2Bot.Application.Interfaces;
using Metin2Bot.Domain.Models;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Metin2Bot.Infrastructure.Services
{
    public class VisionService : IVisionService
    {
        #region Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        private const int SRCCOPY = 0x00CC0020;
        #endregion

        public MatchResult? FindTemplate(string templatePath, Domain.Models.Region searchRegion, double threshold = 0.6)
        {
            if (!File.Exists(templatePath))
                return null;

            try
            {
                using Bitmap screenBmp = CaptureRegion(searchRegion);
                using Mat sourceMat = BitmapToMat(screenBmp);
                using Image<Bgr, byte> templateImage = new Image<Bgr, byte>(templatePath);
                using Mat sourceGray = new Mat();
                using Mat templateGray = new Mat();
                using Mat result = new Mat();

                CvInvoke.CvtColor(sourceMat, sourceGray, ColorConversion.Bgr2Gray);
                CvInvoke.CvtColor(templateImage, templateGray, ColorConversion.Bgr2Gray);
                CvInvoke.MatchTemplate(sourceGray, templateGray, result, TemplateMatchingType.CcoeffNormed);

                double minVal = 0, maxVal = 0;
                Point minLoc = new Point(), maxLoc = new Point();
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                return new MatchResult
                {
                    Location = new Location(
                        searchRegion.X + maxLoc.X + (templateImage.Width / 2),
                        searchRegion.Y + maxLoc.Y + (templateImage.Height / 2)),
                    Confidence = maxVal
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VisionService] FindTemplate failed for '{templatePath}': {ex.Message}");
                return null;
            }
        }

        private Mat BitmapToMat(Bitmap bitmap)
        {
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            
            Mat mat = new Mat(bitmap.Height, bitmap.Width, DepthType.Cv8U, 3, bmpData.Scan0, bmpData.Stride);
            Mat result = mat.Clone();
            
            bitmap.UnlockBits(bmpData);
            return result;
        }

        private Bitmap CaptureRegion(Domain.Models.Region region)
        {
            // DPI farklarından etkilenmemek için .NET'in yerleşik ekran yakalama yöntemini kullanıyoruz
            Bitmap bmp = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height), CopyPixelOperation.SourceCopy);
            }
            return bmp;
        }
    }
}

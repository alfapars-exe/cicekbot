using System.Windows;
using System.Windows.Input;

namespace Metin2Bot.UI.Views
{
    public partial class SnipOverlay : Window
    {
        private Point _start;
        private bool _drawing;

        /// <summary>
        /// Kullanıcının çizdiği alan — pencereye göreli (DIP). Caller bunu ekran koordinatına çevirir.
        /// </summary>
        public Rect? SelectedRect { get; private set; }

        public SnipOverlay()
        {
            InitializeComponent();
            KeyDown += OnKeyDown;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SelectedRect = null;
                DialogResult = false;
                Close();
            }
        }

        private void DrawCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            _start = e.GetPosition(DrawCanvas);
            _drawing = true;
            SelectionRect.Visibility = Visibility.Visible;
            UpdateRect(_start, _start);
            DrawCanvas.CaptureMouse();
        }

        private void DrawCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_drawing) return;
            UpdateRect(_start, e.GetPosition(DrawCanvas));
        }

        private void DrawCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || !_drawing) return;

            _drawing = false;
            DrawCanvas.ReleaseMouseCapture();

            var end = e.GetPosition(DrawCanvas);
            var rect = MakeRect(_start, end);

            if (rect.Width < 4 || rect.Height < 4)
            {
                SelectedRect = null;
                DialogResult = false;
            }
            else
            {
                SelectedRect = rect;
                DialogResult = true;
            }
            Close();
        }

        private void UpdateRect(Point a, Point b)
        {
            var r = MakeRect(a, b);
            System.Windows.Controls.Canvas.SetLeft(SelectionRect, r.X);
            System.Windows.Controls.Canvas.SetTop(SelectionRect, r.Y);
            SelectionRect.Width = r.Width;
            SelectionRect.Height = r.Height;
        }

        private static Rect MakeRect(Point a, Point b)
        {
            double x = Math.Min(a.X, b.X);
            double y = Math.Min(a.Y, b.Y);
            double w = Math.Abs(a.X - b.X);
            double h = Math.Abs(a.Y - b.Y);
            return new Rect(x, y, w, h);
        }
    }
}

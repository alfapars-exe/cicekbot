using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Metin2Bot.UI.ViewModels
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public static readonly NullToVisibilityConverter Default = new NullToVisibilityConverter();
        public static readonly NullToVisibilityConverter Inverse = new NullToVisibilityConverter { Invert = true };

        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNull = value is null;
            if (Invert) isNull = !isNull;
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

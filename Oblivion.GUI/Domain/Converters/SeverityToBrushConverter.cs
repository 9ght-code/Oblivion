using Oblivion.GUI.MVVM.Model;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Oblivion.GUI.Domain.Converters
{
    public class SeverityToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AnomalySeverity severity)
            {
                string resourceKey = severity switch
                {
                    AnomalySeverity.High => "OblivionRed",
                    AnomalySeverity.Medium => "OblivionOrange",
                    AnomalySeverity.Low => "OblivionTeal",
                    AnomalySeverity.Info => "OblivionTextMuted",
                    _ => "OblivionTextMuted"
                };

                return Application.Current.FindResource(resourceKey) as Brush ?? Brushes.Gray;
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    public class SeverityToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AnomalySeverity severity)
            {
                string resourceKey = severity switch
                {
                    AnomalySeverity.High => "OblivionRedBg",
                    AnomalySeverity.Medium => "OblivionOrangeBg",
                    AnomalySeverity.Low => "OblivionCard",
                    AnomalySeverity.Info => "OblivionCard",
                    _ => "OblivionCard"
                };

                return Application.Current.FindResource(resourceKey) as Brush ?? Brushes.Transparent;
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

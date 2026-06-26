using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Oblivion.GUI.Domain.Converters
{
    public class HealthStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                string resourceKey = status switch
                {
                    "Clean" => "OblivionGreen",
                    "Low Risk" => "OblivionTeal",
                    "Moderate" => "OblivionOrange",
                    "High Risk" => "OblivionRed",
                    _ => "OblivionTextMuted"
                };

                return Application.Current.FindResource(resourceKey) as Brush ?? Brushes.Gray;
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

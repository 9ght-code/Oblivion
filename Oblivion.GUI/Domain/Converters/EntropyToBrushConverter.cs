using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Oblivion.GUI.Domain.Converters
{
    public class EntropyToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double entropy)
            {
                string resourceKey = entropy switch
                {
                    > 7.0 => "OblivionRed",
                    > 5.0 => "OblivionOrange",
                    _     => "OblivionTeal"
                };

                return Application.Current.FindResource(resourceKey) as Brush ?? Brushes.Gray;
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

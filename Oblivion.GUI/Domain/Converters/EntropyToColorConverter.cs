using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Oblivion.GUI.Domain.Converters
{
    public class EntropyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double entropy)
            {
                string resourceKey;

                if (entropy > 7.0)
                    resourceKey = "OblivionRed";
                else if (entropy > 6.0)
                    resourceKey = "OblivionOrange";
                else
                    resourceKey = "OblivionGreen";

                return Application.Current.FindResource(resourceKey) as Brush ?? Brushes.Gray;
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

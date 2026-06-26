using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Oblivion.GUI.Domain.Converters
{
    public class BoolToPermissionBrushConverter : IValueConverter
    {

        private readonly SolidColorBrush _green = (SolidColorBrush)new BrushConverter().ConvertFrom("#3FB950"); // R
        private readonly SolidColorBrush _red = (SolidColorBrush)new BrushConverter().ConvertFrom("#F85149"); // W
        private readonly SolidColorBrush _blue = (SolidColorBrush)new BrushConverter().ConvertFrom("#2f81f7"); // X
        private readonly SolidColorBrush _gray = (SolidColorBrush)new BrushConverter().ConvertFrom("#30363d");
        private readonly SolidColorBrush _transparent = Brushes.Transparent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSet && isSet)
            {
                string param = parameter as string;

                return param switch
                {
                    "R" => _green,
                    "W" => _red,
                    "X" => _blue,
                    _ => _gray,
                };
            }

            return _transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

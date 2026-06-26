using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Oblivion.GUI.MVVM.Model;

namespace Oblivion.GUI.Domain.Converters
{
    public class InstructionCategoryToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not InstructionCategory category)
                return GetBrush("OblivionText");

            return category switch
            {
                InstructionCategory.Call => GetBrush("OblivionAccent"),
                InstructionCategory.Jump => GetBrush("OblivionOrange"),
                InstructionCategory.Return => GetBrush("OblivionRed"),
                InstructionCategory.Nop => GetBrush("OblivionTextMuted"),
                _ => GetBrush("OblivionText"),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static Brush GetBrush(string key)
        {
            if (Application.Current.Resources[key] is Brush brush)
                return brush;
            return Brushes.White;
        }
    }
}

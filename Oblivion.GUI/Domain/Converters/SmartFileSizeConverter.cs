using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Oblivion.GUI.Domain.Converters
{
    public class SmartFileSizeConverter : IValueConverter
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "—";

            if (!double.TryParse(value.ToString(), out double bytes))
                return "—";

            int unitIndex = 0;

            while (bytes >= 1024 && unitIndex < Units.Length - 1)
            {
                bytes /= 1024;
                unitIndex++;
            }

            // 0 B, 1 KB, 1.23 MB, 12.5 GB
            string format = bytes >= 10 ? "0.#" : "0.##";

            return $"{bytes.ToString(format, culture)} {Units[unitIndex]}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

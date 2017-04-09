using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Diagnostics;

namespace Jarloo.Calendar.Converters
{
    public class DayContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Console.Write("----------------------- DEBUGGER: " + value);
            MessageBox.Show((string)value);
            return string.IsNullOrEmpty((string)value) ? null : "YOOOOOOOOOOOOOOOOOO";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

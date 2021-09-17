using System;
using System.Windows;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace GlobalMediaControl
{
    public class MarqSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Console.WriteLine(value);
            Console.WriteLine(targetType);
            Console.WriteLine(parameter);
            Console.WriteLine(culture);
            Grid label = (Grid)value;
            int offset = (int)(label.ActualWidth - label.RenderSize.Width);
            Console.WriteLine("actual:" + label.ActualWidth);
            Console.WriteLine("render:" + label.RenderSize.Width);
            Console.WriteLine("offset:" + offset);

            return offset;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new Size(0, 0);
        }
    }
}

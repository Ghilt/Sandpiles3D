using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Sandpiles3DWPF.Converters
{
    [ValueConversion(typeof(RelayCommand), typeof(string))]
    public class CommandNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            RelayCommand cmd = value as RelayCommand;
            return cmd.name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new RelayCommand(value as string, null);
        }

    }
}

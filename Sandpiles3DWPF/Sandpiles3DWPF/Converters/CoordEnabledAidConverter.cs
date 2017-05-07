using Sandpiles3DWPF.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Sandpiles3DWPF.Converters
{
    [ValueConversion(typeof(bool), typeof(float))]
    public class CoordEnabledAidConverter : IValueConverter
    {
        private const float DISABLED = 0;
        private const float MINIMIZED = 0.1f;
        private const float ENABLED = 1;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ObservableField<bool>[] coordsEnabled = value as ObservableField<bool>[];
            string[] parameters = parameter.ToString().Split('|');
            bool relevantDimension = coordsEnabled[Int32.Parse(parameters[0])].Value;
            bool scaleDimension = coordsEnabled[Int32.Parse(parameters[1])].Value;
            bool allEnabled = true;
            bool alldisabled = true;
            for (int i = 0; i < coordsEnabled.Length; i++)
            {
                allEnabled &= coordsEnabled[i].Value;
                alldisabled &= !coordsEnabled[i].Value;

            }
            if (allEnabled)
            {
                return MINIMIZED;
            }
            else if (alldisabled)
            {
                return ENABLED;
            }
            else if (relevantDimension && scaleDimension)
            {
                return MINIMIZED;
            }
            else if (relevantDimension)
            {
                return ENABLED;
            }
            else
            {
                return DISABLED;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

}

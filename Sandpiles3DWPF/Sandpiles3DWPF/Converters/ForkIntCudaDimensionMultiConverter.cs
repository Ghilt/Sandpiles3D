using Sandpiles3DWPF.Model.Cuda;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Sandpiles3DWPF.Converters
{
    class ForkIntCudaDimensionMultiConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is CudaKernelDimensions)
            {
                return values[0];
            }
            else if (values[0].Equals(values[1]))
            {
                return SandpilesCalculatorCuda.AVAILABLE_CUDA_DIMENSIONS.FirstOrDefault(x => values[0].Equals(x.dataDimensions));
            } else
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (value is CudaKernelDimensions)
            {
                var v = value as CudaKernelDimensions;
                return new object[] { v.dataDimensions, v.dataDimensions};
            } else
            {
                return new object[] { value, value };
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Sandpiles3DWPF.ViewModel
{
    //Todo remove entire class and replace with ValueConverter
    /* Hosts two observable fields reflecting the same value */
    public class ObservableStringAccessor<T> : INotifyPropertyChanged
    {
        private Func<string, T> converter;
        private T observableValue;
        public T Value
        {
            get { return observableValue;}
            set { observableValue = value; OnPropertyChanged(); OnPropertyChanged("StringValue"); }
        }

        public string StringValue /* Property name do not change, used in string literal */
        {
            get { return observableValue != null ? observableValue.ToString() : GetDefaultValue(observableValue.GetType()).ToString(); } // this is unnecessary
            set { observableValue = converter.Invoke(value); OnPropertyChanged(); }
        }

        public ObservableStringAccessor(Func<string, T> c)
        {
            this.converter = c;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if(PropertyChanged != null){
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private T GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return (T) Activator.CreateInstance(t);

            return default(T);
        }

        internal static ObservableStringAccessor<T>[] CreateFields(Func<string, T> c, int count)
        {
            ObservableStringAccessor<T>[] ret = new ObservableStringAccessor<T>[count];
            for (int i = 0; i < count; i++ )
            {
                ret[i] = new ObservableStringAccessor<T>(c);
            }
            return ret;
        }

    }
}

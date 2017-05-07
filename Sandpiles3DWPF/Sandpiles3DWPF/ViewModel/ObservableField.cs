using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sandpiles3DWPF.ViewModel
{
    public class ObservableField<T> : INotifyPropertyChanged
    {

        private T observableValue;
        public T Value
        {
            get { return observableValue != null ? observableValue : GetDefaultValue(observableValue.GetType()); }
            set { observableValue = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if(PropertyChanged != null){
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        internal int ToInt() // extract to subclass
        {
            return Int32.Parse(Value as string);
        }

        private T GetDefaultValue(Type t)
        {
            if (t.IsValueType)
                return (T) Activator.CreateInstance(t);

            return default(T);
        }

        internal static ObservableField<T>[] CreateFields(int count, Action propertyUpdate)
        {
           ObservableField <T>[] ret = new ObservableField<T>[count];
            for (int i = 0; i < count; i++ )
            {
                ObservableField<T> field = new ObservableField<T>(); 
                field.PropertyChanged += (x, y) => propertyUpdate.Invoke();
                ret[i] = field;
            }
            return ret;
        }
    }
}

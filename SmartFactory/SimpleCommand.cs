using System.Windows.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartFactory
{
    public class SimpleCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private Action<object> Do;

        public SimpleCommand(Action<object> action)
        {
            Do = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Do?.Invoke(parameter);
        }
    }
}

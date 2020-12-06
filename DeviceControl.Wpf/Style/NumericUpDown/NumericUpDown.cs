using Prism.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace DeviceControl.Wpf.Style
{
    public class NumericUpDown : TextBox, INotifyPropertyChanged
    {
        // Using a DependencyProperty as the backing store for Value. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));

        // Using a DependencyProperty as the backing store for Maximum. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(int.MaxValue));

        // Using a DependencyProperty as the backing store for Minimum. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(1));

        // Using a DependencyProperty as the backing store for Increment. This enables animation,
        // styling, binding, etc...
        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(int), typeof(NumericUpDown), new PropertyMetadata(1));

        public NumericUpDown()
        {
            IncrementCommand = new DelegateCommand(GoUp);
            DecrementCommand = new DelegateCommand(GoDown);
            WriteValueCommand = new DelegateCommand(WriteValue);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public int Increment
        {
            get => (int)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public DelegateCommand IncrementCommand { get; }

        public DelegateCommand DecrementCommand { get; }

        public DelegateCommand WriteValueCommand { get; }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void GoUp()
        {
            if ((Value += Increment) > Maximum)
            {
                Value = Maximum;
            }
        }

        private void GoDown()
        {
            if ((Value -= Increment) < Minimum)
            {
                Value = Minimum;
            }
        }

        private void WriteValue()
        {
            //if some how there is a entry error and the minimum property became greater than maximum property take the minimum value as first priority
            if (Value < Minimum)
            {
                Value = Minimum;
            }
            else if (Value > Maximum)
            {
                Value = Maximum;
            }
        }
    }
}
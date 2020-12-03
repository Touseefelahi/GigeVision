using Prism.Commands;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace DeviceControl.Wpf.Style
{
    public class NumericUpDown : TextBox, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public long Value
        {
            get { return (long)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(long), typeof(NumericUpDown), new PropertyMetadata(long.MinValue));

        public long Maximum
        {
            get { return (long)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(long), typeof(NumericUpDown), new PropertyMetadata(long.MaxValue));

        public long Minimum
        {
            get { return (long)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Minimum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(long), typeof(NumericUpDown), new PropertyMetadata(long.MinValue));

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
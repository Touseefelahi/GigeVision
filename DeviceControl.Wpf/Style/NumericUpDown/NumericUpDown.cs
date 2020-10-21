using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace DeviceControl.Wpf.Style
{
    public class NumericUpDown : TextBox, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set
            {
                if (Value > Maximum)
                    SetValue(ValueProperty, Maximum);
                else if (Value < Minimum)
                    SetValue(ValueProperty, Minimum);
                else
                    SetValue(ValueProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for Value.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown), new PropertyMetadata(0));

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Maximum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(100));

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Minimum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(1));

        public int Increment
        {
            get { return (int)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Increment.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(int), typeof(NumericUpDown), new PropertyMetadata(1));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == NumericUpDown.ValueProperty)
            {
                if (Value > Maximum && Value > Minimum)
                    SetValue(ValueProperty, Maximum);
                else if (Value < Minimum && Value < Minimum)
                    SetValue(ValueProperty, Minimum);
                else
                    SetValue(ValueProperty, Value);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public NumericUpDown()
        {
            IncrementCommand = new DelegateCommand(GoUp);
            DecrementCommand = new DelegateCommand(GoDown);
        }

        public DelegateCommand IncrementCommand { get; }
        public DelegateCommand DecrementCommand { get; }

        private void GoUp()
        {
            if ((Value += Increment) > Maximum)
                Value = Maximum;
        }

        private void GoDown()
        {
            if ((Value -= Increment) < Minimum)
                Value = Minimum;
        }
    }
}
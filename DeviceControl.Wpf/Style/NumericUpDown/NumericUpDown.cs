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
            set { SetValue(ValueProperty, value); }
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
            DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown), new PropertyMetadata(int.MaxValue));

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
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public NumericUpDown()
        {
            IncrementCommand = new DelegateCommand(GoUp);
            DecrementCommand = new DelegateCommand(GoDown);
            WriteValueCommand = new DelegateCommand(WriteValue);
        }

        public DelegateCommand IncrementCommand { get; }
        public DelegateCommand DecrementCommand { get; }
        public DelegateCommand WriteValueCommand { get; }

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

        private void WriteValue()
        {
            //if some how there is a entry error and the minimum property became greater than maximum property take the minimum value as first priority
            if (Value < Minimum)
                Value = Minimum;
            else if (Value > Maximum)
                Value = Maximum;
        }
    }
}
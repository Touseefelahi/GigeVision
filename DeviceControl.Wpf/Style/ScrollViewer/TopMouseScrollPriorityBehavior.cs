using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DeviceControl.Wpf.Style
{
    public class TopMouseScrollPriorityBehavior
    {
        public static bool GetTopMouseScrollPriority(DependencyObject obj)
        {
            return (bool)obj.GetValue(TopMouseScrollPriorityProperty);
        }

        public static void SetTopMouseScrollPriority(DependencyObject obj, bool value)
        {
            obj.SetValue(TopMouseScrollPriorityProperty, value);
        }

        public static readonly DependencyProperty TopMouseScrollPriorityProperty =
            DependencyProperty.RegisterAttached("TopMouseScrollPriority", typeof(bool), typeof(TopMouseScrollPriorityBehavior), new PropertyMetadata(false, OnPropertyChanged));

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as ScrollViewer;
            if (scrollViewer == null)
                throw new InvalidOperationException($"{nameof(TopMouseScrollPriorityBehavior)}.{nameof(TopMouseScrollPriorityProperty)} can only be applied to controls of type {nameof(ScrollViewer)}");
            if (e.NewValue == e.OldValue)
                return;
            if ((bool)e.NewValue)
                scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            else
                scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
        }

        private static void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scrollViewer = (ScrollViewer)sender;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
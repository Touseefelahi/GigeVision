using GigeVision.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GigeVision.Wpf.Views
{
    /// <summary>
    /// Interaction logic for DeviceControl.xaml
    /// </summary>
    public partial class DeviceControl : UserControl
    {
        //public CameraRegisterVisibility CameraRegisterVisibility
        //{
        //    get { return (CameraRegisterVisibility)GetValue(CameraRegisterVisibilityProperty); }
        //    set { SetValue(CameraRegisterVisibilityProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for CameraRegisterVisibility.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty CameraRegisterVisibilityProperty =
        //    DependencyProperty.Register("CameraRegisterVisibility", typeof(CameraRegisterVisibility), typeof(DeviceControl), new PropertyMetadata(CameraRegisterVisibility.Beginner));

        //public ICommand LoadedWindowCommand
        //{
        //    get { return (ICommand)GetValue(LoadedWindowCommandProperty); }
        //    set { SetValue(LoadedWindowCommandProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for LoadedWindowCommand.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty LoadedWindowCommandProperty =
        //    DependencyProperty.Register("LoadedWindowCommand", typeof(ICommand), typeof(DeviceControl), new PropertyMetadata(null));

        public DeviceControl()
        {
            InitializeComponent();
            //this.Loaded += DeviceControl_Loaded;
        }

        //private void DeviceControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    LoadedWindowCommand?.Execute(null);
        //}
    }
}
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
        public CameraRegisterVisibilty CameraRegisterVisibilty
        {
            get { return (CameraRegisterVisibilty)GetValue(CameraRegisterVisibiltyProperty); }
            set { SetValue(CameraRegisterVisibiltyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CameraRegisterVisibilty.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CameraRegisterVisibiltyProperty =
            DependencyProperty.Register("CameraRegisterVisibilty", typeof(CameraRegisterVisibilty), typeof(DeviceControl), new PropertyMetadata(CameraRegisterVisibilty.Beginner));

        public ICommand LoadedWindowCommand
        {
            get { return (ICommand)GetValue(LoadedWindowCommandProperty); }
            set { SetValue(LoadedWindowCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LoadedWindowCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LoadedWindowCommandProperty =
            DependencyProperty.Register("LoadedWindowCommand", typeof(ICommand), typeof(DeviceControl), new PropertyMetadata(null));

        public DeviceControl()
        {
            InitializeComponent();
            this.Loaded += DeviceControl_Loaded;
        }

        private void DeviceControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadedWindowCommand?.Execute(null);
        }
    }
}
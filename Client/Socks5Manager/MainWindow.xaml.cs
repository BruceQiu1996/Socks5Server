using Microsoft.Extensions.DependencyInjection;
using Socks5Manager.ViewModels;
using System;
using System.Windows;

namespace Socks5Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Border_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void Label_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Label_MouseLeftButtonDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var add = App.ServiceProvider.GetRequiredService<AddWindow>();
            var vm = App.ServiceProvider.GetRequiredService<AddWindowViewModel>();
            vm.ExpireTime = DateTime.Now;
            add.DataContext = vm;
            add.ShowDialog();
        }
    }
}

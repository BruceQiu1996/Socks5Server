using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using System.IO;
using System.Net;
using System.Windows;

namespace Socks5Client.ViewModels
{
    public class SettingsPageViewModel : ObservableObject
    {
        private string _socks5Host;
        public string Socks5Host
        {
            get => _socks5Host;
            set => SetProperty(ref _socks5Host, value);
        }

        private uint _socks5Port;
        public uint Socks5Port
        {
            get => _socks5Port;
            set => SetProperty(ref _socks5Port, value);
        }

        private uint _localPort;
        public uint LocalPort
        {
            get => _localPort;
            set => SetProperty(ref _localPort, value);
        }

        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public AsyncRelayCommand<PasswordBox> SaveSettingsCommandAsync { get; set; }
        public RelayCommand<PasswordBox> LoadedCommand { get; set; }
        public SettingsPageViewModel()
        {
            SaveSettingsCommandAsync = new AsyncRelayCommand<PasswordBox>(SaveSettingsAsync);
            LoadedCommand = new RelayCommand<PasswordBox>((PasswordBox) =>
            {
                Socks5Host = App.SettingsModel.Socks5Host;
                Socks5Port = App.SettingsModel.Socks5Port;
                LocalPort = App.SettingsModel.LocalPort;
                UserName = App.SettingsModel.UserName;
                PasswordBox.Password = App.SettingsModel.Password;
            });
        }

        private async Task SaveSettingsAsync(PasswordBox passwordBox)
        {
            if (!string.IsNullOrEmpty(Socks5Host) && !IPAddress.TryParse(Socks5Host, out _))
            {
                HandyControl.Controls.MessageBox.Show("IP地址格式错误", "错误", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return;
            }

            if (Socks5Port <= 0 || Socks5Port > 65535)
            {
                HandyControl.Controls.MessageBox.Show("端口号错误", "错误", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return;
            }

            await App.SettingsModel.WriteSocks5PortAsync(Socks5Port);
            await App.SettingsModel.WriteSocks5HostAsync(Socks5Host);
            await App.SettingsModel.WriteUserNameAsync(UserName);
            await App.SettingsModel.WritePasswordAsync(passwordBox.Password);
            await App.SettingsModel.WritLocalPortAsync(LocalPort);
        }
    }
}

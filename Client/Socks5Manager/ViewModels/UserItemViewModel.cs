using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using Microsoft.Extensions.DependencyInjection;
using Socks5Manager.Http;
using System;
using System.Threading.Tasks;

namespace Socks5Manager.ViewModels
{
    public class UserItemViewModel : ObservableObject
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsOnline { get; set; }
        public string IsOnlineText => IsOnline ? "在线" : "不在线";
        public long UploadBytes { get; set; }
        public long DownloadBytes { get; set; }
        public DateTime ExpireTime { get; set; }
        public string UploadBytesText => ConvertBytesToDisplayText(UploadBytes);
        public string DownloadBytesText => ConvertBytesToDisplayText(DownloadBytes);
        public RelayCommand EditCommand { get; set; }
        public string ExpireTimeText => ExpireTime == DateTime.MaxValue ? "永久" : ExpireTime.ToString("yyyy-MM-dd");

        public UserItemViewModel()
        {
            EditCommand = new RelayCommand(Edit);
        }

        public void Edit()
        {
            var edit = App.ServiceProvider.GetRequiredService<EditWindow>();
            var editWindowViewModel = App.ServiceProvider.GetRequiredService<EditWindowViewModel>();
            editWindowViewModel.UserId = UserId;
            editWindowViewModel.UserName = UserName;
            editWindowViewModel.ExpireTime = ExpireTime;
            edit.DataContext = editWindowViewModel;

            edit.ShowDialog();
        }

        private string ConvertBytesToDisplayText(long bytes)
        {
            if (bytes >= 0 && bytes < 1024)
            {
                return $"{bytes}B";
            }
            else if (bytes >= 1024 && bytes < 1024 * 1024)
            {
                return $"{bytes * 1.0 / 1024:0.0}KB";
            }
            else if (bytes >= 1024 * 1024 && bytes < 1024 * 1024 * 1024)
            {
                return $"{bytes * 1.0 / (1024 * 1024):0.0}MB";
            }
            else
            {
                return $"{bytes * 1.0 / (1024 * 1024 * 1024):0.0}GB";
            }
        }
    }
}

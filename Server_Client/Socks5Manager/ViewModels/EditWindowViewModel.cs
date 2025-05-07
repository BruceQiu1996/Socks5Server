using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Socks5Manager.Http;
using System;
using System.Threading.Tasks;

namespace Socks5Manager.ViewModels
{
    public class EditWindowViewModel : ObservableObject
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        private DateTime _expireTime;
        public DateTime ExpireTime 
        {
            get => _expireTime;
            set => SetProperty(ref _expireTime, value);
        }

        private string _password;
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }
        public AsyncRelayCommand UpdateUserCommandAsync { get; set; }

        private readonly ILogger<EditWindowViewModel> _logger;
        public EditWindowViewModel(ILogger<EditWindowViewModel> logger)
        {
            _logger = logger;
            UpdateUserCommandAsync = new AsyncRelayCommand(UpdateUserAsync);
        }

        private async Task UpdateUserAsync()
        {
            try
            {
                var httpService = App.ServiceProvider.GetRequiredService<ServerHttpService>();
                await httpService.PutAsync("/account", new
                {
                    UserId,
                    Password,
                    ExpireTime
                });
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.ToString());
            }
        }
    }
}

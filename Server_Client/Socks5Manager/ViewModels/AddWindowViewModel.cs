using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Socks5Manager.Http;
using System;
using System.Threading.Tasks;

namespace Socks5Manager.ViewModels
{
    public class AddWindowViewModel : ObservableObject
    {
        private string _userName;
        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

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

        public AsyncRelayCommand AddCommandAsync { get; set; }

        private readonly ILogger<AddWindowViewModel> _logger;
        public AddWindowViewModel(ILogger<AddWindowViewModel> logger)
        {
            _logger = logger;
            AddCommandAsync = new AsyncRelayCommand(AddAsync);
        }

        private async Task AddAsync() 
        {
            try
            {
                var httpService = App.ServiceProvider.GetRequiredService<ServerHttpService>();
                await httpService.PostAsync("/account", new
                {
                    UserName,
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

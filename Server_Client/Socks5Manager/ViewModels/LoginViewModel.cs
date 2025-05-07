using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HandyControl.Controls;
using HandyControl.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Socks5Manager.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Socks5Manager.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        public string _userName;
        public string UserName
        {
            get { return _userName; }
            set { SetProperty(ref _userName, value); }
        }

        public AsyncRelayCommand<PasswordBox> LoginCommandAsync { get; set; }
        private readonly IConfiguration _configuration;
        private readonly ServerHttpService _serverHttpService;

        public LoginViewModel(IConfiguration configuration, ServerHttpService serverHttpService)
        {
            _configuration = configuration;
            _serverHttpService = serverHttpService;
            LoginCommandAsync = new AsyncRelayCommand<PasswordBox>(LoginAsync);
        }

        private async Task LoginAsync(PasswordBox password) 
        {
            var result = await _serverHttpService.PostAsync("account/login", new 
            {
                UserName = UserName,
                password.Password
            });

            if (result.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<UserToken>(await result.Content.ReadAsStringAsync());
                _serverHttpService.SetToken($"{data.Token}");
                App.ServiceProvider.GetRequiredService<MainWindow>().Show();
                App.ServiceProvider.GetRequiredService<Login>().Close();
            }
            else 
            {
                Growl.WarningGlobal(new GrowlInfo()
                {
                    WaitTime = 5,
                    Message = "登录失败",
                    ShowDateTime = false
                });
            }
        }
    }

    public class UserToken 
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}

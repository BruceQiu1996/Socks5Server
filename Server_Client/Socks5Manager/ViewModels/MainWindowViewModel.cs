using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Socks5Manager.Http;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Socks5Manager.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private ObservableCollection<UserItemViewModel> users;
        public ObservableCollection<UserItemViewModel> Users
        {
            get => users;
            set => SetProperty(ref users, value);
        }

        private UserItemViewModel user;
        public UserItemViewModel User
        {
            get => user;
            set => SetProperty(ref user, value);
        }

        public AsyncRelayCommand LoadCommandAsync { get; set; }

        private readonly ServerHttpService _serverHttpService;
        private readonly ILogger<MainWindowViewModel> _logger;

        public MainWindowViewModel(ServerHttpService serverHttpService, ILogger<MainWindowViewModel> logger)
        {
            _logger = logger;
            _serverHttpService = serverHttpService;
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            Users = new ObservableCollection<UserItemViewModel>();
        }

        private async Task LoadAsync()
        {
            Users.Clear();
            User = null;
            try
            {
                var resp = await _serverHttpService.GetAsync("account/accounts");
                if (resp.IsSuccessStatusCode)
                {
                    var users =
                        JsonSerializer.Deserialize<IEnumerable<UserItemViewModel>>(await resp.Content.ReadAsStringAsync(), new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true
                        });
                    users.OrderByDescending(x => x.IsOnline).ToList().ForEach(x => Users.Add(x));
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex.ToString());
            }
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Socks5Manager.Http;
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

        public AsyncRelayCommand LoadCommandAsync { get; set; }

        private readonly ServerHttpService _serverHttpService;
        public MainWindowViewModel(ServerHttpService serverHttpService)
        {
            _serverHttpService = serverHttpService;
            LoadCommandAsync = new AsyncRelayCommand(LoadAsync);
            Users = new ObservableCollection<UserItemViewModel>();
        }

        private async Task LoadAsync()
        {
            Users.Clear();
            var resp = await _serverHttpService.GetAsync("account/accounts");
            if (resp.IsSuccessStatusCode)
            {
                var users = JsonSerializer.Deserialize<IEnumerable<UserItemViewModel>>(await resp.Content.ReadAsStringAsync());

                users.OrderByDescending(x => x.IsOnline).ToList().ForEach(x => Users.Add(x));

            }
        }
    }
}

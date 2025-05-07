using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Socks5Client.ViewModels
{
    public class MainPageViewModel : ObservableObject
    {
        public AsyncRelayCommand OpenProxyCommandAsync { get; set; }
        public MainPageViewModel()
        {
            OpenProxyCommandAsync = new AsyncRelayCommand(OpenProxyAsync);
        }

        private async Task OpenProxyAsync() 
        {
            try
            {
                SystemProxy.SetProxy("127.0.0.1:8080", true);
            }
            catch (Exception ex) 
            {
            
            }
        }
    }
}

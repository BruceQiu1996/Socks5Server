using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Socks5Client.Pages;
using System.Windows.Controls;

namespace Socks5Client.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private Page _currentPage;
        public Page CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        private readonly MainPage _mainPage;
        private readonly SettingsPage _settingsPage;

        public RelayCommand OpenSettingsPageCommand { get; set; }
        public MainWindowViewModel(MainPage mainPage,SettingsPage settingsPage)
        {
            _settingsPage = settingsPage;
            _mainPage = mainPage;
            CurrentPage = _mainPage;
            OpenSettingsPageCommand = new RelayCommand(() =>
            {
                CurrentPage = _settingsPage;
            });
        }
    }
}

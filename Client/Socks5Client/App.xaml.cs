using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Socks5Client.Pages;
using Socks5Client.ViewModels;
using System.IO;
using System.Windows;

namespace Socks5Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>

    public partial class App : Application
    {
        internal static IServiceProvider? ServiceProvider;
        internal static IHost host;
        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var builder = Host.CreateDefaultBuilder(e.Args); ;

            builder.ConfigureServices((context, service) =>
            {
                service.AddSingleton<MainWindow>();
                service.AddSingleton<MainWindowViewModel>();

                service.AddSingleton<MainPage>();
                service.AddSingleton<SettingsPage>();
            });

            host = builder.Build();
            ServiceProvider = host.Services;
            await host.StartAsync();
            host.Services.GetRequiredService<MainWindow>().Show();
        }
    }

}

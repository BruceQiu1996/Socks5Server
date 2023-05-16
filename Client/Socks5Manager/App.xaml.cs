using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Socks5Manager.Http;
using Socks5Manager.ViewModels;
using System;
using System.Windows;

namespace Socks5Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider;

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var hostbuilder = CreateHostBuilder(e.Args);
            var host = await hostbuilder.StartAsync();
            ServiceProvider = host.Services;
            host.Services.GetRequiredService<MainWindow>()?.Show();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args).UseSerilog((context, logger) =>//注册Serilog
            {
                logger.ReadFrom.Configuration(context.Configuration);
                logger.Enrich.FromLogContext();
            });
            hostBuilder.ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<ServerHttpService>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<Login>();
                services.AddSingleton<LoginViewModel>();
                services.AddTransient<EditWindow>();
                services.AddTransient<EditWindowViewModel>();
                services.AddTransient<AddWindow>();
                services.AddTransient<AddWindowViewModel>();
            });

            return hostBuilder;
        }
    }
}

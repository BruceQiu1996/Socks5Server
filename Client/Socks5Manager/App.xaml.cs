using HandyControl.Themes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Socks5Manager.Http;
using Socks5Manager.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

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
            host.Services.GetRequiredService<Login>()?.Show();
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
            });

            return hostBuilder;
        }
    }
}

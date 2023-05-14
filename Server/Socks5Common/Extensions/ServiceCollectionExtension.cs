using Microsoft.Extensions.DependencyInjection;
using Socks5Common.State;

namespace Socks5Common.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void AddSocks5(this IServiceCollection services) 
        {
            services.AddTransient<ClientStateMachine>();
            services.AddSingleton<Socks5ByteUtil>();
        }
    }
}

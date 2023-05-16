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

        /// <summary>
        /// 解决构造方法循环引用
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddLazyResolution(this IServiceCollection services)
        {
            return services.AddTransient(
                typeof(Lazy<>),
                typeof(LazilyResolved<>));
        }

        private class LazilyResolved<T> : Lazy<T>
        {
            public LazilyResolved(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<T>)
            {
            }
        }
    }
}

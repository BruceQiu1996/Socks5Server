using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Socks5_Server.Configuration;
using Socks5_Server.Data;
using Socks5_Server.Services;
using Socks5Common.Extensions;
using Socks5Common.State;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Socks5_Server
{
    internal class Program
    {
        internal static IServiceProvider ServiceProvider { get; private set; }

        async static Task Main(string[] args)
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            var host = CreateHostBuilder(args).Build();
            ServiceProvider = host.Services;
            host.UseSwagger();
            host.UseSwaggerUI();
            host.MapControllers();
            host.UseAuthentication();
            host.UseAuthorization();

            await host.RunAsync();
        }

        public static WebApplicationBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = WebApplication.CreateBuilder(args);
            hostBuilder.Host.UseSerilog((context, logger) =>//Serilog
            {
                string date = DateTime.Now.ToString("yyyy-MM-dd");//按时间创建文件夹
                logger.ReadFrom.Configuration(context.Configuration);
                logger.Enrich.FromLogContext();
                logger.WriteTo.File($"Logs/{date}/", rollingInterval: RollingInterval.Hour);//按小时分日志文件
            });

            hostBuilder.Host.ConfigureServices((hostContext, services) =>
            {
                AddJwtAuthentication(services, hostContext.Configuration);
                services.Configure<ServerConfiguration>(hostContext.Configuration.GetSection("ServerConfig"));
                services.Configure<JwtConfiguration>(hostContext.Configuration.GetSection("Jwt"));
                services.AddDbContextFactory<Socks5ServerDbContext>(option =>
                {
                    option.EnableSensitiveDataLogging(false);
                    option.UseSqlite($"Data Source={Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data/data.db")}");
                });
                services.AddSingleton<IUserService, UserService>();
                services.AddSingleton<ClientConnectionManager>();
                services.AddSingleton<IClientStateChangeHandler, ClientStateChangeHandler>();
                services.AddHostedService<Socks5ProxyService>();
                services.AddControllers();
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen();
                services.AddSocks5();
                services.AddLazyResolution();
            });

            return hostBuilder;
        }

        public static void AddJwtAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var jwtConfig = configuration.GetSection("Jwt").Get<JwtConfiguration>();
            var keyByteArray = Encoding.ASCII.GetBytes(jwtConfig.Secret);
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            //认证参数
            services.AddAuthentication("Bearer")
                .AddJwtBearer(o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,//是否验证签名,不验证的画可以篡改数据，不安全
                        IssuerSigningKey = signingKey,//解密的密钥
                        ValidateIssuer = true,//是否验证发行人，就是验证载荷中的Iss是否对应ValidIssuer参数
                        ValidIssuer = jwtConfig.Iss,//发行人
                        ValidateAudience = true,//是否验证订阅人，就是验证载荷中的Aud是否对应ValidAudience参数
                        ValidAudience = jwtConfig.Aud,//订阅人
                        ValidateLifetime = true,//是否验证过期时间，过期了就拒绝访问
                        ClockSkew = TimeSpan.Zero,//这个是缓冲过期时间，也就是说，即使我们配置了过期时间，这里也要考虑进去，过期时间+缓冲，默认好像是7分钟，你可以直接设置为0
                        RequireExpirationTime = true,
                    };
                });
        }
    }
}
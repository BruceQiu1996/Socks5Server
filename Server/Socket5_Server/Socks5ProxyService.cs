using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Socks5_Server.Configuration;
using Socks5Common;
using Socks5Common.State;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Socks5_Server
{
    public class Socks5ProxyService : BackgroundService
    {
        Socket _tcpSocket;
        private readonly ILogger<Socks5ProxyService> _logger;
        private readonly ServerConfiguration _serverConfiguration;
        private readonly Socks5ByteUtil _byteUtil;
        private readonly ClientConnectionManager _clientConnectionManager;

        public Socks5ProxyService(IOptions<ServerConfiguration> _options,
                                  ILogger<Socks5ProxyService> logger, 
                                  Socks5ByteUtil byteUtil, 
                                  ClientConnectionManager clientConnectionManager)
        {
            _logger = logger;
            _byteUtil = byteUtil;
            _serverConfiguration = _options.Value;
            _clientConnectionManager = clientConnectionManager;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, _serverConfiguration.Port);
            _tcpSocket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _tcpSocket.Bind(ep);
            _tcpSocket.Listen();
            while (true)
            {
                Socket client = await _tcpSocket.AcceptAsync();
                var stateMachine = Program.ServiceProvider.GetRequiredService<ClientStateMachine>();
                await stateMachine.StartAsync();
                ProcessReceiveClient(client, stateMachine);
            }
        }

        public void ProcessReceiveClient(Socket client, ClientStateMachine stateMachine)
        {
            UserToken token = new UserToken
            {
                ClientSocket = client,
                ClientBuffer = new byte[8 * 1024], //8kb
                ClientStateMachine = stateMachine
            };
            async Task Process()
            {
                _clientConnectionManager.AddClient(token);
                while (true)
                {
                    try
                    {
                        int length = await client.ReceiveAsync(token.ClientBuffer);
                        if (length == 0) //收到长度为0的数据包，一般表示客户端将要断开
                        {
                            throw new Exception("收到socket断开数据包");
                        }

                        token.ClientData = token.ClientBuffer.AsMemory(0, length);
                        switch (token.ClientStateMachine.GetState())
                        {
                            case ClientState.Normal:
                                await token.ClientStateMachine.NormalGoToBeCertified(token);
                                break;
                            case ClientState.ToBeCertified:
                                await token.ClientStateMachine.ToBeCertifiedGoCertified(token);
                                break;
                            case ClientState.Certified:
                                await token.ClientStateMachine.CertifiedGoConnected(token);
                                break;
                            case ClientState.Connected:
                                await ForwardAsync(token);
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception)
                    {
                        token.Dispose();
                        _clientConnectionManager.RemoveClient(token);
                        break;
                    }
                }
            }

            Task.Run(Process, token.CancellationTokenSource.Token);
        }

        /// <summary>
        /// 使用tcp协议将客户端数据转发给对应代理服务
        /// </summary>
        /// <param name="token"></param>
        private async Task ForwardAsync(UserToken token)
        {
            if (token.ClientStateMachine.GetState() == ClientState.Connected)
            {
                await token.ForwardTcpAsync();
            }
        }
    }
}

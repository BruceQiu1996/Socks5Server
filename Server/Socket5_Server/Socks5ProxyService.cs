﻿using Microsoft.Extensions.DependencyInjection;
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
        Socket _udpSocket;//udp转发数据
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
            if (_serverConfiguration.SupportUpd)
            {
                _udpSocket = new Socket(ep.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                _udpSocket.Bind(ep);
                UdpReceiveProcessAsync();
            }
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
                            throw new SocketException();
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
                    catch (SocketException)
                    {
                        token.Dispose();
                        _clientConnectionManager.RemoveClient(token);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"{nameof(ProcessReceiveClient)}|{ex}");
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
            try
            {
                if (token.ClientStateMachine.GetState() == ClientState.Connected)
                {
                    await token.ServerSocket.SendAsync(token.ClientData, SocketFlags.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{nameof(ForwardAsync)}|{ex}");
            }
        }

        /// <summary>
        /// 开启udp循环接收数据
        /// </summary>
        private void UdpReceiveProcessAsync()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    EndPoint point = new IPEndPoint(IPAddress.Any, 0);//用来保存发送方的ip和端口号
                    Memory<byte> buffer = new byte[1024 * 8];
                    var result = await _udpSocket.ReceiveFromAsync(buffer, point);
                    var data = buffer.Slice(0, result.ReceivedBytes);
                    var client = _clientConnectionManager.FindFirstOrDefault(c => c.ClientSocket.RemoteEndPoint.Equals(point) && c.ClientSocket.ProtocolType == ProtocolType.Udp);
                    int header_len = 0;
                    if (client != null && client.ClientStateMachine.GetState() == ClientState.Connected && client.IsSupportUdp)
                    {
                        var proxyInfo = _byteUtil.GetProxyInfo(data.Slice(2));
                        switch (proxyInfo.Item1)
                        {
                            case Socks5AddressType.IPV4://IPV4
                                header_len = 10;
                                break;
                            case Socks5AddressType.Domain://域名
                                header_len = 7 + data.Span[4];
                                break;
                            case Socks5AddressType.IPV6://IPV6
                                header_len = 22;
                                break;
                        }

                        await client.SendToTargetByUdpAsync(data.Slice(header_len), new IPEndPoint(proxyInfo.Item2, proxyInfo.Item3));
                    }
                }
            });
        }
    }
}
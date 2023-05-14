using Microsoft.Extensions.Options;
using Socks5_Server.Configuration;
using Socks5_Server.Services;
using Socks5Common;
using Socks5Common.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Socks5_Server
{
    public class ClientStateChangeHandler : IClientStateChangeHandler
    {
        byte _exceptionCode = 0xFF;
        private readonly ServerConfiguration _serverConfiguration;
        private readonly Socks5ByteUtil _byteUtil;
        private readonly IUserService _userService;

        public ClientStateChangeHandler(IOptions<ServerConfiguration> _options,
                                        Socks5ByteUtil byteUtil,
                                        IUserService userService)
        {
            _byteUtil = byteUtil;
            _serverConfiguration = _options.Value;
            NeedAuth = _serverConfiguration.NeedAuth;
            _userService = userService;
        }

        public bool NeedAuth { get; set; }

        /// <summary>
        /// 处理认证协商
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task HandleAuthenticationNegotiationRequestAsync(UserToken token)
        {
            if (token.ClientData.Length < 3)
            {
                await token.ClientSocket.SendAsync(new byte[] { 0x05, _exceptionCode });
                throw new ArgumentException("Error request format from client.");
            }
            if (token.ClientData.Span[0] != 0x05) //socks5默认头为5
            {
                await token.ClientSocket.SendAsync(new byte[] { 0x05, _exceptionCode });
                throw new ArgumentException("Error request format from client.");
            }
            int methodCount = token.ClientData.Span[1];
            if (token.ClientData.Length < 2 + methodCount) //校验报文
            {
                await token.ClientSocket.SendAsync(new byte[] { 0x05, _exceptionCode });
                throw new ArgumentException("Error request format from client.");
            }
            bool supprtAuth = false;
            for (int i = 0; i < methodCount; i++)
            {
                if (token.ClientData.Span[2 + i] == 0x02)
                {
                    supprtAuth = true;
                    break;
                }
            }

            if (_serverConfiguration.NeedAuth && !supprtAuth) //是否支持账号密码认证
            {
                await token.ClientSocket.SendAsync(new byte[] { 0x05, _exceptionCode });
                throw new InvalidOperationException("Can't support password authentication!");
            }

            await token.ClientSocket.SendAsync(new byte[] { 0x05, (byte)(_serverConfiguration.NeedAuth ? 0x02 : 0x00) });
        }

        /// <summary>
        /// 接收到客户端认证
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task HandleClientProfileAsync(UserToken token)
        {
            var version = token.ClientData.Span[0];
            //if (version != _serverConfiguration.AuthVersion)
            //{
            //    await token.ClientSocket.SendAsync(new byte[] { 0x05, _exceptionCode });
            //    throw new ArgumentException("The certification version is inconsistent");
            //}

            var userNameLength = token.ClientData.Span[1];
            var passwordLength = token.ClientData.Span[2 + userNameLength];
            if (token.ClientData.Length < 3 + userNameLength + passwordLength)
            {
                await token.ClientSocket.SendAsync(new byte[] { 0x05, _exceptionCode });
                throw new ArgumentException("Error authentication format from client.");
            }

            var userName = Encoding.UTF8.GetString(token.ClientData.Span.Slice(2, userNameLength));
            var password = Encoding.UTF8.GetString(token.ClientData.Span.Slice(3 + userNameLength, passwordLength));
            var user = await _userService.FindSingleUserByUserNameAndPasswordAsync(userName, password);
            if (user == null || user.ExpireTime < DateTime.Now) 
            {
                await token.ClientSocket.SendAsync(new byte[] { version, _exceptionCode });
                throw new ArgumentException($"User{userName}尝试非法登录");
            }

            token.UserName = user.UserName;
            token.Password = user.Password;
            token.ExpireTime = user.ExpireTime;
            await token.ClientSocket.SendAsync(new byte[] { version, 0x00 });
        }

        /// <summary>
        /// 客户端请求连接
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task HandleRequestProxyAsync(UserToken token)
        {
            var data = token.ClientData.Slice(3);
            Socks5CommandType socks5CommandType = (Socks5CommandType)token.ClientData.Span[1];
            var proxyInfo = _byteUtil.GetProxyInfo(data);
            var serverPort = BitConverter.GetBytes(_serverConfiguration.Port);
            if (socks5CommandType == Socks5CommandType.Connect) //tcp
            {
                //返回连接成功
                IPEndPoint targetEP = new IPEndPoint(proxyInfo.Item2, proxyInfo.Item3);//目标服务器的终结点
                token.ServerSocket = new Socket(targetEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                token.ServerSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                var e = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = new IPEndPoint(targetEP.Address, targetEP.Port)
                };
                token.ServerSocket.ConnectAsync(e);
                e.Completed += async (e, a) =>
                {
                    token.ServerBuffer = new byte[800 * 1024];//800kb
                    token.StartTcpProxy();
                    var datas = new List<byte> { 0x05, 0x0, 0, (byte)Socks5AddressType.IPV4 };
                    foreach (var add in (token.ServerSocket.LocalEndPoint as IPEndPoint).Address.GetAddressBytes())
                    {
                        datas.Add(add);
                    }
                    //代理端启动的端口信息回复给客户端
                    datas.AddRange(BitConverter.GetBytes((token.ServerSocket.LocalEndPoint as IPEndPoint).Port).Take(2).Reverse());

                    await token.ClientSocket.SendAsync(datas.ToArray());
                };
            }
            else if (socks5CommandType == Socks5CommandType.Udp)//udp
            {
                token.ClientUdpEndPoint = new IPEndPoint(proxyInfo.Item2, proxyInfo.Item3);//客户端发起代理的udp终结点
                token.IsSupportUdp = true;
                token.ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                token.ServerSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
                token.ServerBuffer = new byte[800 * 1024];//800kb
                token.StartUdpProxy();
                await token.ClientSocket.SendAsync(new byte[] { 0x05, 0x0, 0, (byte)Socks5AddressType.IPV4, 0, 0, 0, 0, serverPort[1], serverPort[0] });
            }
            else
            {
                await token.ClientSocket.SendAsync(new byte[] { 0x05, 0x1, 0, (byte)Socks5AddressType.IPV4, 0, 0, 0, 0, 0, 0 });
                throw new Exception("Unsupport proxy type.");
            }
        }

    }
}

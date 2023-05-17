using Socks5Common;
using Socks5Common.State;
using System.Net;
using System.Net.Sockets;

namespace Socks5_Server
{
    /// <summary>
    /// 客户端请求连接元数据集
    /// </summary>
    public class UserToken : IDisposable
    {
        private bool _disposed;
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool IsSupportUdp { get; set; } //是否是使用了udp协议进行代理
        public IPEndPoint ClientUdpEndPoint { get; set; }
        public Socket ClientSocket { get; set; } //客户端请求代理服务器的socket tcp协议
        public byte[] ClientBuffer { get; set; }
        public Memory<byte> ClientData { get; set; }
        public Socket ServerSocket { get; set; } //服务端代理请求目标端的socket tcp或udp
        public byte[] ServerBuffer { get; set; } //代理服务器转发数据到本地的buffer，设置大小可以控制网速
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTime ExpireTime { get; set; } = DateTime.MaxValue;
        //public long UploadBytes { get; set; }
        //public long DownloadBytes { get; set; }
        public ClientStateMachine ClientStateMachine { get; set; } //单个请求代理连接的状态机
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();//结束绑定的相关任务
        public Action<string, long> ExcuteAfterUploadBytes { get; set; }
        public Action<string, long> ExcuteAfterDownloadBytes { get; set; }
        /// <summary>
        /// 开启udp代理将数据发送到客户端
        /// </summary>
        /// <param name="sourceIPEndPoint"></param>
        public void StartUdpProxy(Socks5ByteUtil byteUtil)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (ClientStateMachine?.GetState() == ClientState.Connected && IsSupportUdp)
                    {
                        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                        var dataLength = ServerSocket.ReceiveFrom(ServerBuffer, ref endPoint);
                        if (dataLength == 0)
                        {
                            Dispose();
                        }
                        var data = ServerBuffer.AsMemory(0, dataLength);
                        if ((endPoint as IPEndPoint).Equals(ClientUdpEndPoint))
                        {
                            //解析报文发送给目标端
                            var proxyInfo = byteUtil.GetProxyInfo(data.Slice(3));
                            var sendData = proxyInfo.Item1 switch
                            {
                                Socks5AddressType.IPV4 => data.Slice(10),
                                Socks5AddressType.Domain => data.Slice(proxyInfo.Item4 + 4),
                                Socks5AddressType.IPV6 => data.Slice(22),
                                _ => throw new NotSupportedException(),
                            };
                            await ServerSocket.SendToAsync(sendData, new IPEndPoint(proxyInfo.Item2, proxyInfo.Item3));
                            if (!string.IsNullOrEmpty(UserName))
                                ExcuteAfterUploadBytes?.Invoke(UserName, dataLength);
                        }
                        else
                        {
                            await ServerSocket.SendToAsync(data, ClientUdpEndPoint);
                            if (!string.IsNullOrEmpty(UserName))
                                ExcuteAfterDownloadBytes?.Invoke(UserName, dataLength);
                        }


                    }
                }
            }, CancellationTokenSource.Token);
        }

        /// <summary>
        /// 开启tcp代理将服务端数据发送到客户端
        /// </summary>
        /// <param name="token"></param>
        public void StartTcpProxy()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var data = await ServerSocket.ReceiveAsync(ServerBuffer);
                    if (data == 0)
                    {
                        Dispose();
                    }

                    await ClientSocket.SendAsync(ServerBuffer.AsMemory(0, data));
                    if (!string.IsNullOrEmpty(UserName))
                        ExcuteAfterDownloadBytes?.Invoke(UserName, data);
                }
            }, CancellationTokenSource.Token);
        }

        /// <summary>
        /// 过期自动下线
        /// </summary>
        public void WhenExpireAutoOffline()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (DateTime.Now > ExpireTime)
                    {
                        Dispose();
                    }

                    await Task.Delay(1000);
                }
            }, CancellationTokenSource.Token);
        }

        public async Task ForwardUdpAsync(ReadOnlyMemory<byte> data, IPEndPoint endPoint)
        {
            await ServerSocket.SendToAsync(data, endPoint);
            if (!string.IsNullOrEmpty(UserName))
                ExcuteAfterUploadBytes?.Invoke(UserName, data.Length);
        }

        /// <summary>
        /// 转发消息向前
        /// </summary>
        /// <returns></returns>
        public async Task ForwardTcpAsync()
        {
            await ServerSocket.SendAsync(ClientData, SocketFlags.None);
            if (!string.IsNullOrEmpty(UserName))
                ExcuteAfterUploadBytes?.Invoke(UserName, ClientData.Length);
        }

        //更新密码或者过期时间后
        public void UpdateUserPasswordAndExpireTime(string password, DateTime dateTime)
        {
            if (password != Password)
            {
                Dispose();
            }

            if (DateTime.Now > ExpireTime)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!_disposed)
                {
                    ClientSocket?.Close();
                    ServerSocket?.Close();
                    ClientSocket?.Dispose();
                    ServerSocket?.Dispose();
                    CancellationTokenSource?.Cancel();
                    ClientStateMachine?.FireException().GetAwaiter().GetResult();
                    _disposed = true;
                }
            }
        }
    }

    public enum Socks5CommandType : byte
    {
        Connect = 1,
        Bind = 2,
        Udp = 3
    }
}

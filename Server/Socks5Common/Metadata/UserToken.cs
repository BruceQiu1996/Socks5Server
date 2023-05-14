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
        public long UploadBytes { get; set; }
        public long DownloadBytes { get; set; }
        public ClientStateMachine ClientStateMachine { get; set; } //单个请求代理连接的状态机
        public CancellationTokenSource CancellationTokenSource { get; set; } = new CancellationTokenSource();//结束绑定的相关任务

        /// <summary>
        /// 开启udp代理将数据发送到客户端
        /// </summary>
        /// <param name="sourceIPEndPoint"></param>
        public void StartUdpProxy()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (ClientStateMachine?.GetState() == ClientState.Connected && IsSupportUdp)
                    {
                        var length = await ServerSocket.ReceiveAsync(ServerBuffer);
                        if (length == 0)
                        {
                            Dispose();
                        }
                        await ServerSocket.SendToAsync(ServerBuffer.AsMemory(0, length), ClientUdpEndPoint);//发送到源
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

        public async Task SendToTargetByUdpAsync(ReadOnlyMemory<byte> data, IPEndPoint endPoint)
        {
            await ServerSocket.SendToAsync(data, endPoint);
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

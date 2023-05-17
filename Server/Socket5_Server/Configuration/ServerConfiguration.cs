namespace Socks5_Server.Configuration
{
    public class ServerConfiguration
    {
        public ushort Port { get; set; }
        public byte AuthVersion { get; set; }
        public bool NeedAuth { get; set; } //是否开启认证
    }
}

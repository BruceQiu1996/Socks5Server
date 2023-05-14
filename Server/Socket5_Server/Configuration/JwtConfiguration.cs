namespace Socks5_Server.Configuration
{
    public class JwtConfiguration
    {
        public string Secret { get; set; }
        public string Iss { get; set; }//发行人
        public string Aud { get; set; }//订阅人
    }
}

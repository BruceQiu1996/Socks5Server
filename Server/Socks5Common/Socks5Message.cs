namespace Socks5Common
{
    public class Socks5Message
    {
        /// <summary>
        /// 无需验证
        /// </summary>
        public byte[] No_Authentication_Required = new byte[] { 5, 0 };

        public byte[] Connect_Fail = new byte[] { 5, 255 };

        /// <summary>
        /// 需要身份验证
        /// </summary>
        public byte[] Authentication_Required = new byte[] { 5, 2 };

        /// <summary>
        /// 身份验证成功
        /// </summary>
        public byte[] Authentication_Success = new byte[] { 1, 0 };

        public byte[] Proxy_Success = new byte[] { 5, 0, 0, 1, 0, 0, 0, 0, 0, 0 };
        public byte[] Proxy_Error = new byte[] { 5, 1, 0, 1, 0, 0, 0, 0, 0, 0 };
    }
}

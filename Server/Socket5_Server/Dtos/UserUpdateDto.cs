using System;

namespace Socks5_Server.Dtos
{
    public class UserUpdateDto
    {
        public string UserId { get; set; }
        public DateTime ExpireTime { get; set; }
        public string Password { get; set; }
    }
}

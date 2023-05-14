using System;

namespace Socks5_Server.Dtos
{
    public class UserCreationDto
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}

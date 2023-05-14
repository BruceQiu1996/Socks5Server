using System.ComponentModel.DataAnnotations;

namespace Socks5_Server.Dtos
{
    public class UserLoginDto
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
    }
}

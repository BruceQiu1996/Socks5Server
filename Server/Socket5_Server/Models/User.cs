using System;
using System.ComponentModel.DataAnnotations;

namespace Socks5_Server.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [MaxLength(20)]
        public string UserName { get; set; }
        [MaxLength(50)]
        public string Password { get; set; }
        //max永不过期
        //min封号
        public DateTime ExpireTime { get; set; } = DateTime.MaxValue;
        public long UploadBytes { get; set; }
        public long DownloadBytes { get; set; }
        public Role Role { get; set; }
    }

    public enum Role : byte
    {
        Admin = 99,
        user = 1
    }
}

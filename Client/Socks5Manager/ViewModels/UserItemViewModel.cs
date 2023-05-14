using System;

namespace Socks5Manager.ViewModels
{
    public class UserItemViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public bool IsOnline { get; set; }
        public long UploadBytes { get; set; }
        public long DownloadBytes { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}

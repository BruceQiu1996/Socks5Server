using System.Runtime.InteropServices;
using System.Text;

namespace Socks5Client.ViewModels
{
    public class SettingsModel
    {
        private readonly IniSettingsService _iniSettingsService;
        private readonly string _section = "application";
        public SettingsModel(string path)
        {
            _iniSettingsService = new IniSettingsService(path);
        }

        public string Socks5Host { get; set; }
        public uint Socks5Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        public async Task WriteSocks5HostAsync(string host) 
        {
            await _iniSettingsService.WriteAsync(_section,nameof(Socks5Host),host);
            Socks5Host =  host;
        }

        public async Task WriteSocks5PortAsync(uint port)
        {
            await _iniSettingsService.WriteAsync(_section, nameof(Socks5Port), port.ToString());
            Socks5Port = port;
        }

        public async Task WriteUserNameAsync(string userName)
        {
            await _iniSettingsService.WriteAsync(_section, nameof(UserName), userName);
            UserName = userName;
        }

        public async Task WritePasswordAsync(string password)
        {
            await _iniSettingsService.WriteAsync(_section, nameof(Password), password);
            Password = password;
        }
    }

    public class IniSettingsService
    {
        public IniSettingsService(string configPath)
        {
            _configFile = configPath;
        }

        private readonly string _configFile;

        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);

        [DllImport("kernel32")]
        private static extern int WritePrivateProfileString(string lpApplicationName, string lpKeyName, string lpString, string lpFileName);

        public Task<string> ReadAsync(string section, string key)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetPrivateProfileString(section, key, null, sb, 1024, _configFile);
            return Task.FromResult(sb.ToString());
        }

        public Task WriteAsync(string section, string key, string value)
        {
            return Task.FromResult(WritePrivateProfileString(section, key, value, _configFile));
        }
    }
}

using System.Runtime.InteropServices;

public class SystemProxy
{
    // 引入Windows API
    [DllImport("wininet.dll")]
    public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    public const int INTERNET_OPTION_REFRESH = 37;

    // 设置系统代理
    public static void SetProxy(string proxyServer, bool enable)
    {
        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        const string keyName = userRoot + "\\" + subkey;

        // 设置代理服务器地址和端口
        Microsoft.Win32.Registry.SetValue(keyName, "ProxyServer", proxyServer);

        // 启用或禁用代理
        Microsoft.Win32.Registry.SetValue(keyName, "ProxyEnable", enable ? 1 : 0);

        // 通知系统设置已更改
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
    }

    // 获取当前代理设置
    public static string GetCurrentProxy()
    {
        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        const string keyName = userRoot + "\\" + subkey;

        var proxyServer = Microsoft.Win32.Registry.GetValue(keyName, "ProxyServer", "") as string;
        var proxyEnabled = (int)Microsoft.Win32.Registry.GetValue(keyName, "ProxyEnable", 0) == 1;

        return proxyEnabled ? proxyServer : "Proxy is disabled";
    }

    /// <summary>
    /// 黑名单
    /// </summary>
    /// <param name="exceptions"></param>
    public static void SetProxyExceptions(string exceptions)
    {
        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        const string keyName = userRoot + "\\" + subkey;

        Microsoft.Win32.Registry.SetValue(keyName, "ProxyOverride", exceptions);
    }
}
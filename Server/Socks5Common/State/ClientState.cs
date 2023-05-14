namespace Socks5Common.State
{
    public enum ClientState
    {
        Normal,
        ToBeCertified,
        Certified,
        Connected,
        Death
    }

    public enum ClientStateEvents
    {
        OnRevAuthenticationNegotiation, //当收到客户端认证协商
        OnRevClientProfile, //收到客户端的认证信息
        OnRevRequestProxy, //收到客户端的命令请求请求代理
        OnException,
        OnDeath
    }
}

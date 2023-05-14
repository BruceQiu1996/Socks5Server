using Socks5_Server;

namespace Socks5Common.State
{
    public interface IClientStateChangeHandler
    {
        public bool NeedAuth { get; set; }
        Task HandleAuthenticationNegotiationRequestAsync(UserToken token);
        Task HandleClientProfileAsync(UserToken token);
        Task HandleRequestProxyAsync(UserToken token);
    }
}

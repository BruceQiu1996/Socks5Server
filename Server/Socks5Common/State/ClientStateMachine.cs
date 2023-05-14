using Appccelerate.StateMachine;
using Appccelerate.StateMachine.AsyncMachine;
using Microsoft.Extensions.Logging;
using Socks5_Server;

namespace Socks5Common.State
{
    public class ClientStateMachine
    {
        private ClientState _currentState;
        private readonly AsyncPassiveStateMachine<ClientState, ClientStateEvents> _machine;
        private readonly ILogger<ClientStateMachine> _logger;

        public ClientStateMachine(IClientStateChangeHandler clientStatehandler,
                                  ILogger<ClientStateMachine> logger)
        {
            _logger = logger;
            var builder = new StateMachineDefinitionBuilder<ClientState, ClientStateEvents>();

            if (clientStatehandler.NeedAuth)
            {
                builder.In(ClientState.Normal)
                    .On(ClientStateEvents.OnRevAuthenticationNegotiation)
                    .Goto(ClientState.ToBeCertified)
                    .Execute<UserToken>(clientStatehandler.HandleAuthenticationNegotiationRequestAsync)
                    .On(ClientStateEvents.OnException)
                    .Goto(ClientState.Death);
            }
            else 
            {
                builder.In(ClientState.Normal)
                        .On(ClientStateEvents.OnRevAuthenticationNegotiation)
                        .Goto(ClientState.Certified)
                        .Execute<UserToken>(clientStatehandler.HandleAuthenticationNegotiationRequestAsync)
                        .On(ClientStateEvents.OnException)
                        .Goto(ClientState.Death);
            }

            builder.In(ClientState.ToBeCertified)
                .On(ClientStateEvents.OnRevClientProfile)
                .Goto(ClientState.Certified)
                .Execute<UserToken>(clientStatehandler.HandleClientProfileAsync)
                .On(ClientStateEvents.OnException)
                .Goto(ClientState.Death); ;

            builder.In(ClientState.Certified)
                .On(ClientStateEvents.OnRevRequestProxy)
                .Goto(ClientState.Connected)
                .Execute<UserToken>(clientStatehandler.HandleRequestProxyAsync)
                .On(ClientStateEvents.OnException)
                .Goto(ClientState.Death);

            _currentState = ClientState.Normal;
            builder.WithInitialState(ClientState.Normal);
            _machine = builder.Build().CreatePassiveStateMachine();
            _machine.TransitionCompleted += (obj, e) =>
            {
                _currentState = e.NewStateId;
            };

            //请求代理状态切换中出现异常，则直接切断socket连接，要求客户端从头开始进行流程
            //TODO服务端处理连接
            _machine.TransitionExceptionThrown += async (obj, e) =>
            {
                _logger.LogError(e.Exception.ToString());
                await _machine.Fire(ClientStateEvents.OnException);
                _currentState = ClientState.Death;
            };
        }

        public async Task StartAsync()
        {
            await _machine.Start();
        }

        public ClientState GetState()
        {
            return _currentState;
        }

        public async Task NormalGoToBeCertified(UserToken userToken)
        {
            await _machine.Fire(ClientStateEvents.OnRevAuthenticationNegotiation, userToken);
        }

        public async Task ToBeCertifiedGoCertified(UserToken userToken)
        {
            await _machine.Fire(ClientStateEvents.OnRevClientProfile, userToken);
        }

        public async Task CertifiedGoConnected(UserToken userToken)
        {
            await _machine.Fire(ClientStateEvents.OnRevRequestProxy, userToken);
        }
    }
}

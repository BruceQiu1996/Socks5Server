using Socks5Common.State;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Socks5_Server
{
    /// <summary>
    /// 客户端连接管理
    /// </summary>
    public class ClientConnectionManager
    {
        private readonly ConcurrentBag<UserToken> _clients = new ConcurrentBag<UserToken>();

        public ClientConnectionManager()
        {
            LoopRemoveDeathUserToken();
        }

        private void LoopRemoveDeathUserToken()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var tempClients = _clients.ToList();
                    for (int i = 0; i < tempClients.Count; i++)
                    {
                        if (tempClients[i].ClientStateMachine.GetState() == ClientState.Death)
                        {
                            var temp = tempClients[i];
                            temp.Dispose();
                            _clients.TryTake(out temp);
                        }
                    }
                    await Task.Delay(1000);
                }
            });
        }

        public void AddClient(UserToken token)
        {
            _clients.Add(token);
        }

        public void RemoveClient(UserToken token)
        {
            _clients.TryTake(out token);
        }

        public UserToken FindFirstOrDefault(Func<UserToken, bool> action)
        {
            return _clients.FirstOrDefault(action);
        }

        public IEnumerable<UserToken> Where(Func<UserToken, bool> action)
        {
            return _clients.Where(action);
        }
    }
}

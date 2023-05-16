using Socks5_Server.Models;
using Socks5_Server.Services;
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
        private readonly ConcurrentDictionary<string, long> _uploadBytes = new ConcurrentDictionary<string, long>();
        private readonly ConcurrentDictionary<string, long> _downloadBytes = new ConcurrentDictionary<string, long>();
        private readonly ConcurrentBag<UserToken> _clients = new ConcurrentBag<UserToken>();
        private readonly Lazy<IUserService> _userService;

        public ClientConnectionManager(Lazy<IUserService> userService)
        {
            _userService = userService;
            LoopRemoveDeathUserToken();
            LoopUpdateUserFlowrate();
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

        private void LoopUpdateUserFlowrate()
        {
            Task.Run(async () =>
            {
                while (true)
                {

                    var datas = _uploadBytes.Select(x =>
                    {
                        return new
                        {
                            UserName = x.Key,
                            AddUploadBytes = x.Value,
                            AddDownloadBytes = _downloadBytes.ContainsKey(x.Key) ? _downloadBytes[x.Key] : 0
                        };
                    });

                    if (datas.Count() <= 0
                        || (datas.All(x => x.AddUploadBytes == 0)
                        && datas.All(x => x.AddDownloadBytes == 0)))
                    {
                        await Task.Delay(5000);
                        continue;
                    }
                    var users = await _userService.Value.GetUsersInNamesAsync(datas.Select(x => x.UserName));

                    foreach (var item in datas)
                    {
                        users.FirstOrDefault(x => x.UserName == item.UserName).UploadBytes += item.AddUploadBytes;
                        users.FirstOrDefault(x => x.UserName == item.UserName).DownloadBytes += item.AddDownloadBytes;
                    }

                    await _userService.Value.BatchUpdateUserAsync(users);
                    _uploadBytes.Clear();
                    _downloadBytes.Clear();
                    await Task.Delay(5000);
                }
            });
        }

        public void AddUploadBytes(string userName, long bytes)
        {
            _uploadBytes.AddOrUpdate(userName, bytes, (key, value) => value + bytes);
        }

        public void AddDownloadBytes(string userName, long bytes)
        {
            _downloadBytes.AddOrUpdate(userName, bytes, (key, value) => value + bytes);
        }

        public void AddClient(UserToken token)
        {
            token.ExcuteAfterUploadBytes = AddUploadBytes;
            token.ExcuteAfterDownloadBytes = AddDownloadBytes;
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

        /// <summary>
        /// 获取当前即时的使用流量
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public (long, long) GetImmediateFlowrateByUserName(string userName)
        {
            long upBytes = _uploadBytes.FirstOrDefault(x => x.Key == userName).Value;
            long downBytes = _downloadBytes.FirstOrDefault(x => x.Key == userName).Value;

            return (upBytes, downBytes);
        }

        public void UpdateUserInfo(User user)
        {
            var client = _clients.FirstOrDefault(x => x.UserName == user.UserName);
            if (client != null)
            {
                client.UpdateUserPasswordAndExpireTime(user.Password,user.ExpireTime);
            }
        }
    }
}

using DataLevelDefines;
using ServerControlService.Model;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerControlService.Service
{

    [Serializable]
    public class NoAppInstanceException : Exception
    {
        public NoAppInstanceException() { }
        public NoAppInstanceException(string message) : base(message) { }
        public NoAppInstanceException(string message, Exception inner) : base(message, inner) { }
        protected NoAppInstanceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }

    public class ServerControlManagementService
    {
        protected RedisClient Client { get; private set; }
        public ServerControlManagementService(IRedisServerConfig ServerConfig)
            : this(ServerConfig.Host, ServerConfig.Port, ServerConfig.Password, ServerConfig.Db)
        {
        }
        public ServerControlManagementService(string host, int port, string password = null, long db = 0):
            this(new RedisClient(new RedisEndpoint(host, port, password, db)))
        {
        }

        public ServerControlManagementService(RedisClient Client)
        {
            this.Client = Client;
        }

        public BahamutAppInstance GetMostFreeAppInstance(string appkey)
        {
            var client = Client.As<BahamutAppInstance>();
            var instanceList = client.GetAllItemsFromList(client.Lists[appkey]);
            if(instanceList.Count == 0)
            {
                throw new NoAppInstanceException();
            }
            foreach (var item in instanceList)
            {
                if (client.ContainsKey(item.Id))
                {
                    return item;
                }
            }
            throw new NoAppInstanceException();
        }

        public BahamutAppInstance RegistAppInstance(BahamutAppInstance instance)
        {
            instance.RegistTime = DateTime.Now;
            instance.Id = Guid.NewGuid().ToString();
            var client = Client.As<BahamutAppInstance>();
            var appInstanceList = client.Lists[instance.Appkey];
            appInstanceList.Add(instance);
            client.SetEntry(instance.Id, instance, TimeSpan.FromMinutes(10));
            return instance;
        }

        public void StartKeepAlive(string instanceId)
        {
            var thread = new Thread(() =>
            {
                var client = Client.As<BahamutAppInstance>();
                var time = TimeSpan.FromMinutes(10);
                while (true)
                {
                    client.ExpireEntryIn(instanceId, time);
                }
            });
            thread.IsBackground = false;
            thread.Start();
        }

        public bool AppInstanceOffline(BahamutAppInstance instance)
        {
            var client = Client.As<BahamutAppInstance>();
            var appInstanceList = client.Lists[instance.Appkey];
            client.RemoveEntry(instance.Id);
            return appInstanceList.Remove(instance);
        }
    }
}

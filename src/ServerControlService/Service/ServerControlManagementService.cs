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
        private IRedisClientsManager controlServerServiceClientManager;

        public ServerControlManagementService(IRedisClientsManager controlServerServiceClientManager)
        {
            this.controlServerServiceClientManager = controlServerServiceClientManager;
        }

        public BahamutAppInstance GetMostFreeAppInstance(string appkey)
        {
            using (var Client = controlServerServiceClientManager.GetClient())
            {
                var client = Client.As<BahamutAppInstance>();
                try
                {
                    var instanceList = client.GetAllItemsFromList(client.Lists[appkey]);
                    if (instanceList.Count == 0)
                    {
                        throw new NoAppInstanceException();
                    }
                    foreach (var item in instanceList)
                    {
                        if (Client.ContainsKey(item.Id))
                        {
                            return item;
                        }
                    }
                    throw new NoAppInstanceException();
                }
                catch (Exception)
                {
                    throw new NoAppInstanceException();
                }
            }
                
        }

        public BahamutAppInstance RegistAppInstance(BahamutAppInstance instance)
        {
            using (var Client = controlServerServiceClientManager.GetClient())
            {
                instance.RegistTime = DateTime.UtcNow;
                instance.Id = Guid.NewGuid().ToString();
                var client = Client.As<BahamutAppInstance>();
                var appInstanceList = client.Lists[instance.Appkey];
                appInstanceList.Add(instance);
                Client.SetValue(instance.Id, DateTime.UtcNow.Ticks.ToString(), TimeSpan.FromMinutes(10));
                return instance;
            }
        }

        public void StartKeepAlive(string instanceId)
        {
            var thread = new Thread(() =>
            {
                using (var Client = controlServerServiceClientManager.GetClient())
                {
                    var time = TimeSpan.FromMinutes(1);
                    while (true)
                    {
                        try
                        {
                            Client.ExpireEntryIn(instanceId, time);

                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Expire Instance Error");
                        }
                        
                        Thread.Sleep((int)(time.TotalMilliseconds * 3 / 4));
                    }
                }
            });
            thread.IsBackground = false;
            thread.Start();
        }

        public bool AppInstanceOffline(BahamutAppInstance instance)
        {
            using (var Client = controlServerServiceClientManager.GetClient())
            {
                var client = Client.As<BahamutAppInstance>();
                var appInstanceList = client.Lists[instance.Appkey];
                client.RemoveEntry(instance.Id);
                return appInstanceList.Remove(instance);
            }
        }
    }
}

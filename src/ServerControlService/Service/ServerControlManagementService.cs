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

        public BahamutAppInstance GetMostFreeAppInstance(string appkey,string region = "default")
        {
            using (var Client = controlServerServiceClientManager.GetClient())
            {
                var client = Client.As<BahamutAppInstance>();
                try
                {
                    var instanceList = client.Lists[appkey];
                    var instances = from s in instanceList where s.Region == region select s;
                    if (instances.Count() == 0)
                    {
                        throw new NoAppInstanceException();
                    }
                    BahamutAppInstance freeInstance = null;
                    foreach (var item in instances)
                    {
                        var ins = client.GetValue(item.Id);
                        if (ins != null)
                        {
                            if (freeInstance == null)
                            {
                                freeInstance = ins;
                            }
                        }
                        else
                        {
                            client.Lists[appkey].Remove(item);
                        }
                        
                    }
                    if (freeInstance == null)
                    {
                        throw new NoAppInstanceException();
                    }
                    freeInstance.OnlineUsers++;
                    return client.GetAndSetValue(freeInstance.Id, freeInstance);
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
                client.Lists[instance.Appkey].Add(instance);
                client.SetEntry(instance.Id,instance, TimeSpan.FromMinutes(10));
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
                    var client = Client.As<BahamutAppInstance>();
                    while (true)
                    {
                        try
                        {
                            client.ExpireEntryIn(instanceId, time);

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
                client.Lists[instance.Appkey].Remove(instance);
                return client.RemoveEntry(instance.Id);
            }
        }
    }
}

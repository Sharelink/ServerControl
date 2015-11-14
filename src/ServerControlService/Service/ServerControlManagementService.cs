using ServerControlService.Model;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServerControlService.Service
{
    public class KeepAliveObserverEventArgs : EventArgs
    {
        public BahamutAppInstance Instance { get; set; }
        public Exception Exception { get; set; }
    }

    public class KeepAliveObserver
    {
        public event EventHandler<KeepAliveObserverEventArgs> OnExpireError;
        public event EventHandler<KeepAliveObserverEventArgs> OnExpireOnce;

        private void Callback(IAsyncResult ar)
        {
            EventHandler handler = ar.AsyncState as EventHandler;
            if (handler != null)
            {
                handler.EndInvoke(ar);
            }
        }

        internal void DispatchExpireError(BahamutAppInstance instance, Exception ex)
        {
            if (OnExpireError != null)
            {
                OnExpireError.BeginInvoke(this, new KeepAliveObserverEventArgs() {Instance = instance,  Exception = ex }, Callback, OnExpireOnce);
            }
        }

        internal void DispatchExpireOnce(BahamutAppInstance instance)
        {
            if (OnExpireOnce != null)
            {
                OnExpireOnce.BeginInvoke(this, new KeepAliveObserverEventArgs() { Instance = instance }, Callback, OnExpireOnce);
            }
        }
    }

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
        public static int AppInstanceExpireTimeOfMinutes = 1;
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
                    var instanceList = client.Sets[appkey];
                    var instances = from s in instanceList where s.Region == region select s;
                    if (instances.Count() == 0)
                    {
                        throw new NoAppInstanceException();
                    }
                    BahamutAppInstance freeInstance = null;
                    foreach (var item in instances)
                    {
                        if (Client.ContainsKey(item.Id))
                        {
                            if (freeInstance == null)
                            {
                                freeInstance = item;
                            }
                        }
                        else
                        {
                            client.RemoveItemFromSet(client.Sets[appkey], item);
                        }
                    }
                    if (freeInstance == null)
                    {
                        throw new NoAppInstanceException();
                    }
                    freeInstance.OnlineUsers++;
                    return freeInstance;
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
                client.Sets[instance.Appkey].Add(instance);
                Client.Set(instance.Id, "online", TimeSpan.FromMinutes(AppInstanceExpireTimeOfMinutes));
                return instance;
            }
        }

        public bool ReActiveAppInstance(BahamutAppInstance instance)
        {
            using (var Client = controlServerServiceClientManager.GetClient())
            {
                instance.RegistTime = DateTime.UtcNow;
                var client = Client.As<BahamutAppInstance>();
                client.Sets[instance.Appkey].Add(instance);
                try
                {
                    Client.Set(instance.Id, "online", TimeSpan.FromMinutes(AppInstanceExpireTimeOfMinutes));
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public KeepAliveObserver StartKeepAlive(BahamutAppInstance instance)
        {
            var observer = new KeepAliveObserver();
            var thread = new Thread(() =>
            {
                using (var Client = controlServerServiceClientManager.GetClient())
                {
                    var time = TimeSpan.FromMinutes(AppInstanceExpireTimeOfMinutes);
                    while (true)
                    {
                        try
                        {
                            if (Client.ExpireEntryIn(instance.Id, time))
                            {
                                observer.DispatchExpireOnce(instance);
                            }
                            else
                            {
                                observer.DispatchExpireError(instance, new Exception("Expire Instance Error"));
                            }
                        }
                        catch (Exception ex)
                        {
                            observer.DispatchExpireError(instance, ex);
                        }
                        Thread.Sleep((int)(time.TotalMilliseconds * 3 / 4));
                    }
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return observer;
        }

        public bool AppInstanceOffline(BahamutAppInstance instance)
        {
            using (var Client = controlServerServiceClientManager.GetClient())
            {
                return Client.Remove(instance.Id);
            }
        }
    }
}

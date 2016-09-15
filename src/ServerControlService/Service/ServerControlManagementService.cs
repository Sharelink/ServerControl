using ServerControlService.Model;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BahamutCommon;

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

        public void DispatchExpireError(BahamutAppInstance instance, Exception ex)
        {
            try
            {
                if (OnExpireError != null)
                {
                    OnExpireError.DynamicInvoke(this, new KeepAliveObserverEventArgs() { Instance = instance, Exception = ex });
                }
            }
            catch (Exception)
            {
                
            }
            
        }

        public void DispatchExpireOnce(BahamutAppInstance instance)
        {
            if (OnExpireOnce != null)
            {
                OnExpireOnce.DynamicInvoke(this, new KeepAliveObserverEventArgs() { Instance = instance });
            }
        }
    }

    public class NoAppInstanceException : Exception
    {
        public NoAppInstanceException() { }
        public NoAppInstanceException(string message) : base(message) { }
        public NoAppInstanceException(string message, Exception inner) : base(message, inner) { }
    }

    public class ServerControlManagementService
    {
        public static TimeSpan AppInstanceExpireTime = TimeSpan.FromMinutes(1);

        private ConnectionMultiplexer redis;
        

        public ServerControlManagementService(ConnectionMultiplexer redis)
        {
            this.redis = redis;
        }

        public async Task<bool> RegistAppInstanceAsync(BahamutAppInstance instance)
        {
            instance.RegistTime = DateTime.UtcNow;
            instance.Id = Guid.NewGuid().ToString();
            var suc = await redis.GetDatabase().StringSetAsync(instance.GetInstanceIdKey(), instance.ToJson());
            if (suc)
            {
                return await PublishNotifyAsync(instance, BahamutAppInstanceNotification.TYPE_REGIST_APP_INSTANCE);
            }
            return false;
        }

        private async Task<bool> PublishNotifyAsync(BahamutAppInstance instance, string type)
        {
            var notify = BahamutAppInstanceNotification.GenerateNotificationByInstance(instance, type).ToJson();
            var x = await redis.GetSubscriber().PublishAsync(instance.Channel, notify);
            return x > 0;
        }

        public async Task<bool> NotifyAppInstanceOfflineAsync(BahamutAppInstance instance)
        {
            return await PublishNotifyAsync(instance, BahamutAppInstanceNotification.TYPE_INSTANCE_OFFLINE);
        }

        public async Task<bool> ReActiveAppInstance(BahamutAppInstance instance)
        {
            var instanceJson = instance.ToJson();
            var suc = await redis.GetDatabase().StringSetAsync(instance.GetInstanceIdKey(), instanceJson);
            if (suc)
            {
                return await PublishNotifyAsync(instance, BahamutAppInstanceNotification.TYPE_REGIST_APP_INSTANCE);
            }
            return false;
        }

        public async Task<bool> NotifyAppInstanceHeartBeatAsync(BahamutAppInstance instance)
        {
            return await PublishNotifyAsync(instance, BahamutAppInstanceNotification.TYPE_INSTANCE_HEART_BEAT);
        }

        public async Task<BahamutAppInstance> GetAppInstanceAsync(string instanceId)
        {
            var instanceJson = await redis.GetDatabase().StringGetAsync(BahamutAppInstance.GenerateAppInstanceKey(instanceId));
            if (!string.IsNullOrWhiteSpace(instanceJson))
            {
                return BahamutAppInstance.FromJson(instanceJson);
            }
            return null;
        }
        
        public KeepAliveObserver StartKeepAlive(BahamutAppInstance instance)
        {
            var observer = new KeepAliveObserver();
            var thread = new Thread(() =>
            {
                var idKey = instance.GetInstanceIdKey();
                while (true)
                {
                    try
                    {
                        var db = redis.GetDatabase();
                        if (db.KeyExpire(idKey, AppInstanceExpireTime))
                        {
                            observer.DispatchExpireOnce(instance);
                        }else
                        {
                            observer.DispatchExpireError(instance, new Exception("Expire Instance Error"));
                        }
                    }
                    catch (Exception ex)
                    {
                        observer.DispatchExpireError(instance, ex);
                    }
                    Thread.Sleep((int)(AppInstanceExpireTime.TotalMilliseconds * 3 / 4));
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return observer;
        }
    }

    public static class GetServiceExtension
    {
        public static ServerControlManagementService GetServerControlManagementService(this IServiceProvider provider)
        {
            return provider.GetService<ServerControlManagementService>();
        }
    }
}

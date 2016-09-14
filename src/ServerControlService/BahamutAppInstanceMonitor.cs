using ServerControlService.Model;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerControlService
{
    public interface BahamutAppInstanceMonitor
    {
        void OnInstanceRegisted(BahamutAppInstanceNotification state);
        void OnInstanceHeartBeating(BahamutAppInstanceNotification state);
        void OnInstanceOffline(BahamutAppInstanceNotification state);
    }

    public class BahamutAppInsanceMonitorManager
    {
        static private BahamutAppInsanceMonitorManager _instance = new BahamutAppInsanceMonitorManager();
        static public BahamutAppInsanceMonitorManager Instance { get { return _instance; } }

        private ConnectionMultiplexer redis;
        private IDictionary<string,List<BahamutAppInstanceMonitor>> monitors = new Dictionary<string,List<BahamutAppInstanceMonitor>>();
        
        public void InitManager(ConnectionMultiplexer redis)
        {
            this.redis = redis;
        }

        public void RegistMonitor(string interestedAppChannel,BahamutAppInstanceMonitor monitor)
        {
            try
            {
                var list = monitors[interestedAppChannel];
                if(list.Contains(monitor))
                {
                    return;
                }
                list.Add(monitor);
            }
            catch (Exception)
            {
                var list = new List<BahamutAppInstanceMonitor>();
                list.Add(monitor);
                monitors[interestedAppChannel] = list;
            }

            this.redis.GetSubscriber().Subscribe(interestedAppChannel, OnInstanceNotified);
        }

        private void OnInstanceNotified(RedisChannel channel,RedisValue notification)
        {
            try
            {
                var list = monitors[channel];
                var notify = BahamutAppInstanceNotification.FromJson(notification);
                switch (notify.NotifyType)
                {
                    case BahamutAppInstanceNotification.TYPE_INSTANCE_HEART_BEAT:
                        foreach (var monitor in list) { monitor.OnInstanceHeartBeating(notify); }
                        break;
                    case BahamutAppInstanceNotification.TYPE_INSTANCE_OFFLINE:
                        foreach (var monitor in list) { monitor.OnInstanceOffline(notify); }
                        break;
                    case BahamutAppInstanceNotification.TYPE_REGIST_APP_INSTANCE:
                        foreach (var monitor in list) { monitor.OnInstanceRegisted(notify); }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
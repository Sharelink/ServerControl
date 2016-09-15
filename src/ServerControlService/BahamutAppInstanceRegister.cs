using NLog;
using ServerControlService.Model;
using ServerControlService.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerControlService
{
    public class BahamutAppInstanceRegister
    {
        public static ServerControlManagementService ManagementService { get; private set; }

        public static void RegistAppInstance(ServerControlManagementService ManagementService,BahamutAppInstance appInstance)
        {
            if (BahamutAppInstanceRegister.ManagementService == null)
            {
                BahamutAppInstanceRegister.ManagementService = ManagementService;
            }
            Task.Run(async () =>
            {
                try
                {
                    await ManagementService.RegistAppInstanceAsync(appInstance);
                    var observer = ManagementService.StartKeepAlive(appInstance);
                    observer.OnExpireError += KeepAliveObserver_OnExpireError;
                    observer.OnExpireOnce += KeepAliveObserver_OnExpireOnce;
                    LogManager.GetLogger("Main").Info("Bahamut App Instance:" + appInstance.Id.ToString());
                    LogManager.GetLogger("Main").Info("Keep Server Instance Alive To Server Controller Thread Started!");
                }
                catch (Exception ex)
                {
                    LogManager.GetLogger("Main").Error(ex, "Unable To Regist App Instance");
                }
            });
        }

        private static void KeepAliveObserver_OnExpireOnce(object sender, KeepAliveObserverEventArgs e)
        {
            Task.Run(async () =>
            {
                await ManagementService.NotifyAppInstanceHeartBeatAsync(e.Instance);
            });
        }

        private static void KeepAliveObserver_OnExpireError(object sender, KeepAliveObserverEventArgs e)
        {
            Task.Run(async () =>
            {
                LogManager.GetLogger("Main").Error(string.Format("Expire Server Error.Instance:{0}", e.Instance.Id), e);
                await ManagementService.ReActiveAppInstance(e.Instance);
            });
        }
    }
}

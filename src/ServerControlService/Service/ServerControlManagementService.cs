using ServerControlService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerControlService.Service
{
    public class ServerControlManagementService
    {
        private ServerControlDBContext DBContext { get; set; }

        public ServerControlManagementService(string connectionString)
            :this(new ServerControlDBContext(connectionString))
        {
        }

        public ServerControlManagementService(ServerControlDBContext DBContext)
        {
            this.DBContext = DBContext;
        }

        public ServerService GetMostFreeAppService(string appkey)
        {
            var services = from s in DBContext.ServerService where s.Appkey == appkey && s.IsServiceOnline > 0 select s;
            //TODO: select the most free server service

            return services.First();
        }

        public ServerService GetServiceById(int ServiceId)
        {
            var services = from s in DBContext.ServerService where s.ServerId == ServiceId select s;
            return services.First();
        }

        public AppServer AddAppServer(AppServer newAppServer)
        {
            return DBContext.AppServer.Add(newAppServer);
        }

        public ServerService AddServerService(int serverId,ServerService newServerService)
        {
            newServerService.ServerId = serverId;
            return DBContext.ServerService.Add(newServerService);
        }

        public void SaveAllChanges()
        {
            DBContext.SaveChangesAsync();
        }
    }
}

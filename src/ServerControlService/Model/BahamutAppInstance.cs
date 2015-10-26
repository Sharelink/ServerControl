using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerControlService.Model
{
    public class BahamutAppInstance
    {
        public string Id { get; set; }
        public string Appkey { get; set; }
        public string InstanceEndPointIP { get; set; }
        public int InstanceEndPointPort { get; set; }
        public string InstanceServiceUrl { get; set; }
        public string Data { get; set; }
        public int OnlineUsers { get; set; }
        public string Region { get; set; }
        public DateTime RegistTime { get; set; }
    }
}

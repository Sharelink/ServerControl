using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServerControlService.Model
{
    public class BahamutAppInstanceNotification
    {
        public const string TYPE_REGIST_APP_INSTANCE = "RG_APP_INS";
        public const string TYPE_INSTANCE_OFFLINE = "RM_APP_INS";
        public const string TYPE_INSTANCE_HEART_BEAT = "HB_APP_INS";

        public string InstanceId { get; set; }
        public string NotifyType{ get; set; }
        public string Appkey { get; set; }

        static public BahamutAppInstanceNotification GenerateNotificationByInstance(BahamutAppInstance instance,string notifyType)
        {
            return new BahamutAppInstanceNotification
            {
                Appkey = instance.Appkey,
                InstanceId = instance.Id,
                NotifyType = notifyType
            };
        }

        public static BahamutAppInstanceNotification FromJson(string json)
        {
            return JsonConvert.DeserializeObject<BahamutAppInstanceNotification>(json);
        }
    }

    public class BahamutAppInstance
    {
        public string Id { get; set; }
        public string Appkey { get; set; }
        public string InstanceEndPointIP { get; set; }
        public int InstanceEndPointPort { get; set; }
        public string InstanceServiceUrl { get; set; }
        public string InfoForClient { get; set; }
        public string Region { get; set; }
        public DateTime RegistTime { get; set; }

        private string _channel;
        public string Channel
        {
            get {
                return _channel;
            } set
            {
                if (string.IsNullOrWhiteSpace(value) || value.Length > 13)
                {
                    throw new Exception("Channel Must Be A 1-13 Length String, Current Is " + value);
                }
                else
                {
                    _channel = value;
                }
            }
        }

        public string GetTypedChannel(string type)
        {
            if (string.IsNullOrWhiteSpace(Channel))
            {
                throw new Exception("Invalid Channel Value");
            }
            var channel = GenerateAppTypedChannel(type, Channel);
            if (channel.Length > 64)
            {
                throw new Exception("Type Is Too Long, Typed Channel Value Must Be A 1-64 Length String");
            }
            return channel;
        }

        static public string GenerateAppTypedChannel(string prefix, string channel)
        {
            return string.Format("{0}_{1}", prefix, channel);
        }

        public override bool Equals(object obj)
        {
            var instance = obj as BahamutAppInstance;
            if (instance != null)
            {
                return Id == instance.Id && Appkey == instance.Appkey;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public string GetInstanceIdKey()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw new Exception("Id Value Is Empty");
            }
            return GenerateAppInstanceKey(Id);
        }

        static public string GenerateAppInstanceKey(string id)
        {
            return string.Format("bt_app_inst:{0}", id);
        }

        public static BahamutAppInstance FromJson(string json)
        {
            return JsonConvert.DeserializeObject<BahamutAppInstance>(json);
        }
    }
}

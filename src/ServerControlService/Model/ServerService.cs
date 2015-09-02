namespace ServerControlService.Model
{
    using BahamutCommon;
    using System;

    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public class ServerServiceType
    {
        public const int SERVICE_ANY = 0;
        public const int SERVICE_MIX = 1;
        public const int SERVICE_CONTROL = 100;
        public const int SERVICE_APP_API = 101;
        public const int SERVICE_FILE_API = 102;
        public const int SERVICE_MYSQL = 103;
        public const int SERVICE_MONGODB = 104;
        public const int SERVICE_REDIS = 105;
        public const int SERVICE_WWW = 106;
    }

    public class ServiceDocumentModelBase : DocumentModel
    {
        public int ServicePort { get; set; }
    }

    public class ControllServiceDocument : ServiceDocumentModelBase
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class MySQLServiceDocument:ServiceDocumentModelBase
    {
        public string Database { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    [Table("ServerControlDB.ServerService")]
    public partial class ServerService
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ServerServiceId { get; set; }

        [Required]
        [StringLength(45)]
        public string ServiceName { get; set; }

        public int ServerId { get; set; }

        [Required]
        [StringLength(128)]
        public string Appkey { get; set; }

        public int IsServiceOnline { get; set; }

        [Column(TypeName = "timestamp")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreateTime { get; set; }

        [StringLength(1073741823)]
        public string ServiceDocument { get; set; }

        public virtual AppServer AppServer { get; set; }
    }

    public partial class ServerService
    {
        [NotMapped]
        public dynamic ServiceDocumentModel
        {
            get { return ServiceDocumentModelBase.ToDocumentObject(ServiceDocument); }
            set { ServiceDocument = ServiceDocumentModelBase.ToDocument(value); }
        }
    }
}

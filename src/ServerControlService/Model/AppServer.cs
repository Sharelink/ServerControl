namespace ServerControlService.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public class AppServerType
    {
        public const int SERVER_TYPE_ANY_SERVER = 0;
        public const int SERVER_TYPE_MIX_SERVER = 1;
        public const int SERVER_TYPE_CONTROLLER_SERVER = 100;
        public const int SERVER_TYPE_AUTHENTICATIN_SERVER = 101;
        public const int SERVER_TYPE_DB_SERVER = 102;
        public const int SERVER_TYPE_CACHE_SERVER = 103;
        public const int SERVER_TYPE_WEB_SERVER = 104;
    }

    [Table("ServerControlDB.AppServer")]
    public partial class AppServer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public AppServer()
        {
            ServerService = new HashSet<ServerService>();
        }

        public int AppServerId { get; set; }

        [Required]
        [StringLength(45)]
        public string ServerName { get; set; }

        [StringLength(128)]
        public string Domain { get; set; }

        [Required]
        [StringLength(256)]
        public string ServerIP { get; set; }

        public int ServerType { get; set; }

        [Column(TypeName = "timestamp")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreateTime { get; set; }

        [StringLength(1073741823)]
        public string ExtraDocument { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ServerService> ServerService { get; set; }
    }

    public partial class AppServer
    {
        [NotMapped]
        public dynamic ExtraDocumentModel
        {
            get { return ServiceDocumentModelBase.ToDocumentObject(ExtraDocument); }
            set { ExtraDocument = ServiceDocumentModelBase.ToDocument(value); }
        }
    }
}

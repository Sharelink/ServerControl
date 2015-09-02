namespace ServerControlService.Model
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    [MySqlDefines.MySqlDbConfigurationType]
    public class ServerControlDBContext : DbContext
    {

        public ServerControlDBContext(string connString) :
            base(connString)
        {
            
        }

        public virtual DbSet<AppServer> AppServer { get; set; }
        public virtual DbSet<ServerService> ServerService { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppServer>()
                .Property(e => e.ServerName)
                .IsUnicode(false);

            modelBuilder.Entity<AppServer>()
                .Property(e => e.Domain)
                .IsUnicode(false);

            modelBuilder.Entity<AppServer>()
                .Property(e => e.ServerIP)
                .IsUnicode(false);

            modelBuilder.Entity<AppServer>()
                .Property(e => e.ExtraDocument)
                .IsUnicode(false);

            modelBuilder.Entity<AppServer>()
                .HasMany(e => e.ServerService)
                .WithRequired(e => e.AppServer)
                .HasForeignKey(e => e.ServerId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<ServerService>()
                .Property(e => e.ServiceName)
                .IsUnicode(false);

            modelBuilder.Entity<ServerService>()
                .Property(e => e.Appkey)
                .IsUnicode(false);

            modelBuilder.Entity<ServerService>()
                .Property(e => e.ServiceDocument)
                .IsUnicode(false);
        }
    }
}

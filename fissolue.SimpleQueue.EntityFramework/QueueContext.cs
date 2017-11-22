using System.Data.Common;
using System.Data.Entity;
using fissolue.SimpleQueue.FluentNHibernate;

namespace fissolue.SimpleQueue.EntityFramework
{
    class QueueContext:DbContext
    {
        public QueueContext():base()
        {
            this.Configuration.LazyLoadingEnabled = true;
        }

        public QueueContext(string nameOrConnectionString) : base(nameOrConnectionString)
        {
            this.Configuration.LazyLoadingEnabled = true;
        }

        public QueueContext(DbConnection existingConnection, bool contextOwnsConnection) : base(existingConnection,
            contextOwnsConnection)
        {
            this.Configuration.LazyLoadingEnabled = true;
        }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
         //   modelBuilder.Entity<QueueItem>().Property(e => e.AvailableDateTime).HasColumnType("datetime2");
          //  modelBuilder.Entity<QueueItem>().Property(e => e.AcknowledgeDateTime).HasColumnType("datetime2");
            modelBuilder.Entity<QueueItem>()
                .Property(a => a.ModifiedDateTime)
                .IsConcurrencyToken();
            //modelBuilder.Entity<QueueItem>()
            //    .Property(a => a.AcknowledgeDateTime)
            //    .IsConcurrencyToken();
            base.OnModelCreating(modelBuilder);
        }

      
        public DbSet<Queue> Queues { get; set; }
        public DbSet<QueueItem> QueueItems { get; set; }
    }
}
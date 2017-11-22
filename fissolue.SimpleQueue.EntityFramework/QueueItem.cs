using System;

namespace fissolue.SimpleQueue.FluentNHibernate
{
    public class QueueItem
    {
        public QueueItem()
        {
            AckId = Guid.NewGuid();
            CreateDateTime = DateTime.UtcNow;
        }

        public virtual int QueueItemId { get; set; }
        public virtual DateTime ModifiedDateTime { get; set; }
        public virtual DateTime CreateDateTime { get; set; }
        public virtual Guid AckId { get; set; }
        public virtual int Tries { get; set; }
        public virtual DateTime AvailableDateTime { get; set; }

        public virtual byte[] Payload { get; set; }
        public virtual DateTime? AcknowledgeDateTime { get; set; }

        public virtual Queue Queue { get; set; }
        public virtual SerializationTypeEnum SerializationType { get; set; }
    }
}
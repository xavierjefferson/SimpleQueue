using System;
using System.Collections.Generic;

namespace fissolue.SimpleQueue.FluentNHibernate
{
    public class Queue
    {
        public Queue()
        {
            QueueItems = new List<QueueItem>();
            CreateDateTime = DateTime.UtcNow;
        }

        public virtual int QueueId { get; set; }
        public virtual string Name { get; set; }

        public virtual IList<QueueItem> QueueItems { get; set; }
        public virtual DateTime CreateDateTime { get; set; }
    }
}
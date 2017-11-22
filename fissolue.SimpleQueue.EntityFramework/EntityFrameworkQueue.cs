using System;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Data.Entity;

namespace fissolue.SimpleQueue.EntityFramework
{
    public class EntityFrameworkQueue<T> : SerializingQueue<T>
    {
        private readonly TimeSpan _dateOffset = TimeSpan.Zero;

        private QueueContext _qc = null;

        private readonly Func<QueueContext> _getContextFunc = null;

        public EntityFrameworkQueue(string name, string dbNameOrConnectionString,
            LocalOptions<T> opts = null) : this(name, () => new QueueContext(dbNameOrConnectionString), opts)
        {
        }

        private EntityFrameworkQueue(string name, Func<QueueContext> contextFunction, LocalOptions<T> opts = null)
        {
            _getContextFunc = contextFunction;
            LocalOptions = opts ?? new LocalOptions<T>();

            using (var context = _getContextFunc())
            {
                var dQuery = context.Database.SqlQuery<DateTime>("select GETUTCDATE();");
                var dbDate = dQuery.AsEnumerable().First();
                _dateOffset = dbDate.Subtract(DateTime.UtcNow);

                using (var dbContextTransaction = context.Database.BeginTransaction())
                {
                    _myQueue = context.Queues.FirstOrDefault(i => i.Name == name);
                    if (_myQueue == null)
                    {
                        _myQueue = new Queue {Name = name, CreateDateTime = DateNow()};
                        context.Queues.Add(_myQueue);
                        context.SaveChanges();
                    }
                    dbContextTransaction.Commit();
                }
            }
        }

        public EntityFrameworkQueue(string name, LocalOptions<T> opts = null) : this(name, () => new QueueContext(),
            opts)
        {
        }

        public override long GetNewCount()
        {
            using (var context = _getContextFunc())
            {
                return context.QueueItems
                    .Count(i => i.AcknowledgeDateTime == null && i.AvailableDateTime <= DateNow() &&
                                i.Queue.QueueId == QueueId);
            }
        }

        public override void Enqueue(TimeSpan delay, params T[] items)
        {
            using (var context = _getContextFunc())
            {
                
                foreach (var item in items)
                    
                    context.QueueItems.Add(new QueueItem
                    {
                        CreateDateTime = DateNow(),
                        ModifiedDateTime = DateNow(),
                        AvailableDateTime = DateNow().Add(delay),
                        SerializationType = LocalOptions.SerializationType,
                        Payload = SerializeItem(item, LocalOptions.SerializationType),
                        Queue = _myQueue
                    });
                context.SaveChanges();
                
            }
        }


        public override DequeueResult<T> Dequeue(TimeSpan? opts = null)
        {
            var visibility = opts ?? LocalOptions.Visibility;
            return Execute(() =>
            {
                using (var context = _getContextFunc())
                {
                    while (true)
                    {
                        //var p = context.QueueItems .FirstOrDefault();

                        //var queue = context.Queues.FirstOrDefault(i => i.QueueId == QueueId);
                        //if (queue == null)
                        //{
                        //    return null;
                        //}
                        //var item = queue.QueueItems
                        //    .FirstOrDefault(i => i.AcknowledgeDateTime == null &&
                        //                         i.AvailableDateTime <= DateNow() );
                        //var item = context.Queues
                        //    .Where(i => i.QueueId == QueueId)
                        //    .SelectMany(i => i.QueueItems)
                        //    .FirstOrDefault(i => i.AcknowledgeDateTime == null &&
                        //                i.AvailableDateTime <= DateNow());
                        var item = context.QueueItems.Include(i=>i.Queue).FirstOrDefault(
                            i => i.Queue.QueueId == QueueId && i.AcknowledgeDateTime == null &&
                                 i.AvailableDateTime <= DateNow());
                        if (item == null)
                        {
                            return null;
                        }

                        if (item.Tries >= LocalOptions.MaxRetries && LocalOptions.DeadQueue != null)
                        {
                            LocalOptions.DeadQueue.Enqueue(DeserializeItem(item));
                            _Acknowledge(item.AckId, context);
                        }
                        else
                        {
                            item.Tries++;
                            item.AvailableDateTime = DateNow().Add(visibility);
                            item.ModifiedDateTime = DateNow();
                            try
                            {
                                context.SaveChanges();
                                return new DequeueResult<T> {AckId = item.AckId, Data = DeserializeItem(item)};
                            }
                            catch (DbUpdateConcurrencyException ex)
                            {
                                //already dequeued
                            }
                        }
                    }
                }
            });
        }


        public override void Acknowledge(Guid ackId)
        {
            using (var context = _getContextFunc())
            {
                _Acknowledge(ackId, context);
            }
        }

        private void _Acknowledge(Guid ackId, QueueContext context)
        {
            var count = 0;
            var queueItem = context.QueueItems.FirstOrDefault(i => i.AckId == ackId);
            if (queueItem != null)
            {
                queueItem.AcknowledgeDateTime = DateNow();
                queueItem.AvailableDateTime = DateNow();
                queueItem.ModifiedDateTime = DateNow();
                count = context.SaveChanges();
            }
            if (count == 0)
                throw new InvalidOperationException("Unidentified AckId");
        }

        public override void Extend(Guid ackId, TimeSpan? duration = null)
        {
            var myVisibility = duration ?? LocalOptions.Visibility;

            using (var context = _getContextFunc())
            {
                var queueItem =
                    context.QueueItems.FirstOrDefault(i => i.AckId == ackId && i.AvailableDateTime >= DateNow());
                if (queueItem != null)
                {
                    try
                    {
                        queueItem.AvailableDateTime = DateNow().Add(myVisibility);
                        queueItem.ModifiedDateTime = DateNow();
                        context.SaveChanges();
                        return;
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                    }
                }
                throw new InvalidOperationException("Unidentified AckId");
            }
        }

        public override long GetTotalCount()
        {
            using (var context = _getContextFunc())
            {
                return context.QueueItems.Count(i => i.Queue.QueueId == QueueId);
            }
        }

        public override long GetPendingCount()
        {
            using (var context = _getContextFunc())
            {
                return context.QueueItems
                    .Count(i => i.AcknowledgeDateTime == null && i.AvailableDateTime > DateNow() &&
                                i.Queue.QueueId == QueueId);
            }
        }

        private DateTime DateNow()
        {
            return DateTime.UtcNow.Add(_dateOffset);
        }

        public override long GetAcknowledgedCount()
        {
            using (var context = _getContextFunc())
            {
                return context.QueueItems.Count(i => i.AcknowledgeDateTime != null && i.Queue.QueueId == QueueId);
            }
        }

        public override void Purge()
        {
            using (var context = _getContextFunc())
            {
                context.QueueItems.RemoveRange(context.QueueItems);
                context.SaveChanges();
            }
        }


        public static TT Execute<TT>(Func<TT> func)
        {
            while (true)
                try
                {
                    return func();
                }

                catch (SqlException ex)
                {
                    if (ex.ErrorCode != 1205 && ex.ErrorCode != -2146232060)
                        throw;
                }
        }
    }
}
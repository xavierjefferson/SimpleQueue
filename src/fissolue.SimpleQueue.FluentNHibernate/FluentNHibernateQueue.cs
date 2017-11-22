using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Exceptions;
using NHibernate.Linq;
using NHibernate.Tool.hbm2ddl;

namespace fissolue.SimpleQueue.FluentNHibernate
{
    public class FluentNHibernateQueue<T> : SerializingQueue<T>
    {
        private const string AvailableParamName = "visible";
        private const string AckIdParamName = "ackId";
        private const string AcknowledgeDateParamName = "ackDateTime";
        private const string TriesParamName = "tries";
        private const string DateNowParamName = "dateNow";

        private static readonly string CleanQuerySql;
        private static readonly string PingQuerySql;
        private static readonly string AckQuerySql;
        private static readonly string GetUpdateSql;

        private static readonly Dictionary<IPersistenceConfigurer, ISessionFactory> myDictionary =
            new Dictionary<IPersistenceConfigurer, ISessionFactory>();

        private readonly TimeSpan _dateOffset = TimeSpan.Zero;

        static FluentNHibernateQueue()
        {
            var queueItemClassName = typeof(QueueItem).Name;
            var acknowledgeColumnName = ObjectHelper.GetPropertyName<QueueItem>(i => i.AcknowledgeDateTime);
            var ackIdColumnName = ObjectHelper.GetPropertyName<QueueItem>(i => i.AckId);
            var availColumnName = ObjectHelper.GetPropertyName<QueueItem>(i => i.AvailableDateTime);
            var triesColumnName = ObjectHelper.GetPropertyName<QueueItem>(i => i.Tries);
            CleanQuerySql = string.Format("delete from {0} where not {1} is null", queueItemClassName,
                acknowledgeColumnName);
            AckQuerySql = string.Format(
                "update {0} set {1}=:{6} where {2}=:{4} and {3}>:{5} and {1} is null",
                queueItemClassName, acknowledgeColumnName, ackIdColumnName, availColumnName, AckIdParamName,
                AvailableParamName, AcknowledgeDateParamName);
            PingQuerySql = string.Format(
                "update {0} set {1}=:{3} where {6}=:{4} and {1}>:{5} and {2} is null",
                queueItemClassName, availColumnName, acknowledgeColumnName, AvailableParamName, AckIdParamName,
                DateNowParamName, ackIdColumnName);
            GetUpdateSql = string.Format("update {0} set {1}=:{5}, {2}=:{4} where {3} = :ackId and {2}<:{6}",
                queueItemClassName, triesColumnName, availColumnName, ackIdColumnName, AvailableParamName,
                TriesParamName, DateNowParamName);
        }

        public FluentNHibernateQueue(string name, IPersistenceConfigurer configurer, bool buildSchema = false,
            LocalOptions<T> opts = null)
        {
            SessionFactory = CreateSessionFactory(configurer, buildSchema);

            LocalOptions = opts ?? new LocalOptions<T>();

            using (var session = GetStatelessSession())
            {
                var sqlConnection = session.Connection as SqlConnection;
                if (sqlConnection != null)
                {
                    var rx = new Regex("^\\d\\.");
                    if (rx.IsMatch(sqlConnection.ServerVersion))
                    {
                        var dt = Convert.ToDateTime(session.CreateSQLQuery("select getutcdate()").UniqueResult());
                        _dateOffset = dt.Subtract(DateTime.UtcNow);
                    }
                    else
                    {
                        var dt = Convert.ToDateTime(session.CreateSQLQuery("select sysutcdatetime()").UniqueResult());
                        _dateOffset = dt.Subtract(DateTime.UtcNow);
                    }
                }
                using (var tx = session.BeginTransaction())
                {
                    _myQueue = session.Query<Queue>().FirstOrDefault(i => i.Name == name);
                    if (_myQueue == null)
                    {
                        _myQueue = new Queue {Name = name, CreateDateTime = DateNow()};
                        session.Insert(_myQueue);
                    }
                    tx.Commit();
                }
            }
        }

        private ISessionFactory SessionFactory { get; }


        public override void Acknowledge(Guid ackId)
        {
            using (var session = GetStatelessSession())
            {
                _Acknowledge(ackId, session);
            }
        }

        public override void Enqueue(TimeSpan delay, params T[] items)
        {
            using (var session = GetStatelessSession())
            {
                foreach (var item in items)
                    session.Insert(new QueueItem
                    {
                        CreateDateTime = DateNow(),
                        AvailableDateTime = DateNow().Add(delay),
                        SerializationType = LocalOptions.SerializationType,
                        Payload = SerializeItem(item, LocalOptions.SerializationType),
                        Queue = _myQueue
                    });
            }
        }


        public override void Purge()
        {
            using (var session = GetStatelessSession())
            {
                session.CreateQuery(CleanQuerySql).ExecuteUpdate();
            }
        }

        public override long GetAcknowledgedCount()
        {
            using (var session = GetStatelessSession())
            {
                return session.Query<QueueItem>()
                    .Count(i => i.AcknowledgeDateTime != null && i.Queue.QueueId == QueueId);
            }
        }

        public override long GetPendingCount()
        {
            using (var session = GetStatelessSession())
            {
                return session.Query<QueueItem>()
                    .Count(i => i.AcknowledgeDateTime == null && i.AvailableDateTime > DateNow() &&
                                i.Queue.QueueId == QueueId);
            }
        }

        public override long GetNewCount()
        {
            using (var session = GetStatelessSession())
            {
                return session.Query<QueueItem>()
                    .Count(i => i.AcknowledgeDateTime == null && i.AvailableDateTime <= DateNow() &&
                                i.Queue.QueueId == QueueId);
            }
        }

        public override long GetTotalCount()
        {
            using (var session = GetStatelessSession())
            {
                return session.Query<QueueItem>().Count(i => i.Queue.QueueId == QueueId);
            }
        }

        public override DequeueResult<T> Dequeue(TimeSpan? opts = null)
        {
            var visibility = opts ?? LocalOptions.Visibility;
            return Execute(() =>
            {
                using (var session = GetStatelessSession())
                {
                    while (true)
                    {
                        var item = session
                            .Query<QueueItem>()
                            .FirstOrDefault(i => i.AcknowledgeDateTime == null &&
                                                 i.AvailableDateTime <= DateNow() &&
                                                 i.Queue.QueueId == QueueId);
                        if (item != null)
                            if (item.Tries >= LocalOptions.MaxRetries && LocalOptions.DeadQueue != null)
                            {
                                LocalOptions.DeadQueue.Enqueue(DeserializeItem(item));
                                _Acknowledge(item.AckId, session);
                            }
                            else
                            {
                                var query = session.CreateQuery(GetUpdateSql)
                                    .SetParameter(TriesParamName, item.Tries + 1)
                                    .SetDateTime2(AvailableParamName, DateNow().Add(visibility))
                                    .SetParameter(AckIdParamName, item.AckId)
                                    .SetDateTime2(DateNowParamName, DateNow());
                                if (query.ExecuteUpdate() == 1)
                                    return new DequeueResult<T> {AckId = item.AckId, Data = DeserializeItem(item)};
                            }
                        else
                            return null;
                    }
                }
            });
        }

        public override void Extend(Guid ackId, TimeSpan? duration = null)
        {
            var myVisibility = duration ?? LocalOptions.Visibility;

            using (var session = GetStatelessSession())
            {
                var query = session.CreateQuery(PingQuerySql)
                    .SetDateTime2(DateNowParamName, DateNow())
                    .SetDateTime2(AvailableParamName, DateNow().Add(myVisibility))
                    .SetParameter(AckIdParamName, ackId);
                if (query.ExecuteUpdate() == 0)
                    throw new InvalidOperationException("Unidentified AckId");
            }
        }

        object mutex = new object();

        private ISessionFactory CreateSessionFactory(IPersistenceConfigurer config, bool schemaUpdate)
        {
            lock (myDictionary)
            {
                if (myDictionary.ContainsKey(config))
                    return myDictionary[config];
                var fluentConfiguration = Fluently.Configure()
                    .Database(config)
                    .Mappings(m => m.FluentMappings.AddFromAssemblyOf<QueueMap>());
                FluentConfiguration exposeConfiguration;
                if (schemaUpdate)
                {
                    string text;
                    exposeConfiguration = fluentConfiguration
                        .ExposeConfiguration(cfg => new SchemaUpdate(cfg).Execute(i => text = i, true));
                }
                else
                {
                    exposeConfiguration = fluentConfiguration;
                }
                var sessionFactory = exposeConfiguration
                    .BuildSessionFactory();
                myDictionary[config] = sessionFactory;
                return sessionFactory;
            }
        }

        private IStatelessSession GetStatelessSession()
        {
            return SessionFactory.OpenStatelessSession();
        }

        private void _Acknowledge(Guid ackId, IStatelessSession session)
        {
            var query = session.CreateQuery(AckQuerySql)
                .SetDateTime2(AvailableParamName, DateNow())
                .SetDateTime2(AcknowledgeDateParamName, DateNow())
                .SetParameter(AckIdParamName, ackId);
            if (query.ExecuteUpdate() == 0)
                throw new InvalidOperationException("Unidentified AckId");
        }

        private DateTime DateNow()
        {
            return DateTime.UtcNow.Add(_dateOffset);
        }


        public static TT Execute<TT>(Func<TT> func)
        {
            while (true)
                try
                {
                    return func();
                }
                catch (GenericADOException ex)
                {
                    var sqlException = ex.InnerException as SqlException;
                    if (sqlException == null)
                        throw;
                    if (sqlException.ErrorCode != -2146232060)
                        throw;
                }
                catch (SqlException ex)
                {
                    if (ex.ErrorCode != 1205)
                        throw;
                }
        }
    }
}
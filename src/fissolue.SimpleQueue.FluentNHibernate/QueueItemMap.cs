using FluentNHibernate.Mapping;

namespace fissolue.SimpleQueue.FluentNHibernate
{
    internal class QueueItemMap : ClassMap<QueueItem>
    {
        public QueueItemMap()
        {
            Table("QueueItem");
            Id(i => i.QueueItemId).Column("QueueItemId").GeneratedBy.Identity();
            Map(i => i.CreateDateTime).Column("CreateDateTime").Not.Nullable();
            Map(i => i.AckId).Column("AckId").Nullable().Length(40).Index("IX_AckId").Unique();
            Map(i => i.Tries).Column("Tries").Not.Nullable();
            Map(i => i.AvailableDateTime).Column("AvailableDateTime").Not.Nullable();
            Map(i => i.Payload).Column("Payload").Length(2147483647).Not.Nullable();
            Map(i => i.AcknowledgeDateTime).Column("AcknowledgeDateTime").Nullable();
            Map(i => i.SerializationType)
                .Column("SerializationType")
                .Not.Nullable()
                .CustomType<SerializationTypeEnum>();
            References(i => i.Queue).Column("QueueId").Not.Nullable();

            LazyLoad();
        }
    }
}
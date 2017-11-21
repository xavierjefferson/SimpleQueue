using System;
using FluentNHibernate.Mapping;

namespace fissolue.SimpleQueue.FluentNHibernate
{
    internal class QueueMap : ClassMap<Queue>
    {
        public QueueMap()
        {
            Table("Queue");
            Id(i => i.QueueId).Column("QueueId").GeneratedBy.Identity();
            Map(i => i.Name).Length(64).Not.Nullable().Index("IX_Name").Unique();
            Map(i => i.CreateDateTime).Column("CreateDateTime") .Not.Nullable();//.Default(new DateTime(1990, 1, 1).ToString("u"));
            HasMany(i => i.QueueItems).KeyColumn("QueueId").Cascade.All();
            LazyLoad();
        }
    }
}
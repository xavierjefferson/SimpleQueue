namespace fissolue.SimpleQueue.FluentNHibernate
{
    public class LocalOptions<T> : Options<T>
    {
        public LocalOptions()
        {
            SerializationType = SerializationTypeEnum.NewtonsoftJson;
        }

        public SerializationTypeEnum SerializationType { get; set; }
    }
}
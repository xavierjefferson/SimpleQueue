namespace fissolue.SimpleQueue.FluentNHibernate
{
    public class LocalOptions<T> : Options<T>
    {
        public SerializationTypeEnum SerializationType { get; set; }

        public LocalOptions()
        {
            SerializationType = SerializationTypeEnum.NewtonsoftJson; 
        }
    }
}
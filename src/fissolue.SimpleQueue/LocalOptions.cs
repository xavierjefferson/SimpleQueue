namespace fissolue.SimpleQueue
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
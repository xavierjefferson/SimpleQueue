using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace fissolue.SimpleQueue
{
    public abstract class SerializingQueue<T> : ISimpleQueue<T>
    {
        protected readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

        protected readonly DataContractJsonSerializer _dataContractJsonSerializer =
            new DataContractJsonSerializer(typeof(T));

        protected readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(T));

        protected readonly JsonSerializerSettings settings =
            new JsonSerializerSettings {TypeNameHandling = TypeNameHandling.All};

        protected Queue _myQueue;

        protected int QueueId => _myQueue.QueueId;
        protected LocalOptions<T> LocalOptions { get; set; }
        public abstract long GetNewCount();
        public abstract void Enqueue(TimeSpan delay, params T[] items);

        public void Enqueue(params T[] items)
        {
            Enqueue(LocalOptions.Delay, items);
        }

        public abstract DequeueResult<T> Dequeue(TimeSpan? opts = null);
        public abstract void Acknowledge(Guid ackId);
        public abstract void Extend(Guid ackId, TimeSpan? delay = null);
        public abstract long GetTotalCount();
        public abstract long GetPendingCount();
        public abstract long GetAcknowledgedCount();
        public abstract void Purge();

        protected T DeserializeItem(QueueItem item)
        {
            using (var mx = new MemoryStream(item.Payload))
            {
                switch (item.SerializationType)
                {
                    case SerializationTypeEnum.NewtonsoftJson:
                        return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(mx.ToArray()));
                    case SerializationTypeEnum.BinaryFormatter:
                        return (T) _binaryFormatter.Deserialize(mx);
                    case SerializationTypeEnum.Xml:
                        return (T) _xmlSerializer.Deserialize(mx);
                    case SerializationTypeEnum.DataContractJsonSerializer:
                        return (T) _dataContractJsonSerializer.ReadObject(mx);
                    default:
                        throw new ArgumentException(
                            string.Format("Unknown serialization type {0}", item.SerializationType));
                }
            }
        }

        protected byte[] SerializeItem(T item, SerializationTypeEnum serializationType)
        {
            switch (serializationType)
            {
                case SerializationTypeEnum.BinaryFormatter:

                    using (var mx = new MemoryStream())
                    {
                        _binaryFormatter.Serialize(mx, item);
                        return mx.ToArray();
                    }
                case SerializationTypeEnum.NewtonsoftJson:
                    return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, settings));
                case SerializationTypeEnum.DataContractJsonSerializer:
                    using (var mx = new MemoryStream())
                    {
                        _dataContractJsonSerializer.WriteObject(mx, item);
                        return mx.ToArray();
                    }
                case SerializationTypeEnum.Xml:
                    using (var mx = new MemoryStream())
                    {
                        _xmlSerializer.Serialize(mx, item);
                        return mx.ToArray();
                    }
                default:
                    throw new ArgumentException(
                        string.Format("Unknown serialization type {0}", serializationType));
            }
        }
    }
}
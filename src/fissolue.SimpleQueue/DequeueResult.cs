using System;

namespace fissolue.SimpleQueue
{
    public class DequeueResult<T>
    {
        public Guid AckId { get; set; }
        public T Data { get; set; }
    }
}
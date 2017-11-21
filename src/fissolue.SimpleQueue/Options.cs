using System;

namespace fissolue.SimpleQueue
{
    public class Options<T>
    {
        public Options()
        {
            Visibility = new TimeSpan(0, 0, 30);
            Delay = TimeSpan.Zero;
            MaxRetries = 5;
        }

        /// <summary>
        ///     By default, if you don't ack a message within the first 30s after receiving it, it is placed back in the queue so
        ///     it can be fetched again. This is called the visibility window.  Defaults to 30 seconds
        /// </summary>
        public TimeSpan Visibility { get; set; }

        /// <summary>
        ///     When a message is added to a queue, it is immediately available for retrieval. However, there are times when you
        ///     might like to delay messages coming off a queue. ie. if you set delay to be 10, then every message will only be
        ///     available for retrieval 10s after being added.  Defaults to 0
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// </summary>
        public ISimpleQueue<T> DeadQueue { get; set; }

        /// <summary>
        ///     This option only comes into effect if you pass in a deadQueue as shown above. What this means is that if an item is
        ///     popped off the queue maxRetries times (e.g. 5) and not acked, it will be moved to this deadQueue the next time it
        ///     is tried to pop off. You can poll your deadQueue for dead messages much like you can poll your regular queues.
        ///     The payload of the messages in the dead queue are the entire messages returned when.get() ing them from the
        ///     original queue.
        /// </summary>
        public int MaxRetries { get; set; }
    }
}
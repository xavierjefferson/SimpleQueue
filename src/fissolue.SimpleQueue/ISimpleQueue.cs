using System;

namespace fissolue.SimpleQueue
{
    public interface ISimpleQueue<T>
    {
        /// <summary>
        /// </summary>
        /// <returns></returns>
        long GetNewCount();

        /// <summary>
        ///     Enqueue an object to the queue with a specific delay before it's available to dequeue
        /// </summary>
        /// <param name="delay"></param>
        /// <param name="items"></param>
        void Enqueue(TimeSpan delay, params T[] items);

        /// <summary>
        ///     Enqueue an object to the queue
        /// </summary>
        /// <param name="items"></param>
        void Enqueue(params T[] items);

        /// <summary>
        ///     Dequeue an object
        /// </summary>
        /// <returns></returns>
        DequeueResult<T> Dequeue(TimeSpan? opts = null);

        /// <summary>
        ///     After you have received an item from a queue and processed it, you can delete it by calling Acknowledge() with the
        ///     unique ackId returned
        /// </summary>
        /// <param name="ackId"></param>
        void Acknowledge(Guid ackId);

        /// <summary>
        ///     After you have received an item from a queue and you are taking a while to process it, you can Extend() the message
        ///     to tell the queue that you are still alive and continuing to process the message
        /// </summary>
        /// <param name="ackId"></param>
        /// <param name="delay"></param>
        void Extend(Guid ackId, TimeSpan? delay = null);

        /// <summary>
        ///     Returns the total number of messages that has ever been in the queue, including all current messages
        /// </summary>
        /// <returns></returns>
        long GetTotalCount();

        /// <summary>
        ///     Returns the total number of messages that are currently in flight. ie. that have been received but not yet acked
        /// </summary>
        /// <returns></returns>
        long GetPendingCount();

        /// <summary>
        ///     Returns the total number of messages that have been processed correctly in the queue
        /// </summary>
        /// <returns></returns>
        long GetAcknowledgedCount();

        /// <summary>
        ///     Deletes all processed mesages from the queue. Of course, you can leave these hanging around if you wish, but delete
        ///     them if you no longer need them.
        /// </summary>
        void Purge();
    }
}
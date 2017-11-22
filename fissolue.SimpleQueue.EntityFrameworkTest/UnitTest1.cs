using System;
using fissolue.SimpleQueue.EntityFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fissolue.SimpleQueue.EntityFrameworkTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var queueInstance = new EntityFrameworkQueue<int>("test2",
                new LocalOptions<int> {SerializationType = SerializationTypeEnum.DataContractJsonSerializer});
            var rx = new Random();
            for (var x = 0; x < 100; x++)
                queueInstance.Enqueue(rx.Next());
            //queueInstance.Enqueue("hello");
            var dequeueResult = queueInstance.Dequeue();

            queueInstance.Extend(dequeueResult.AckId);
            queueInstance.Acknowledge(dequeueResult.AckId);
            var newCount = queueInstance.GetNewCount();
            for (var i = 0; i < newCount; i++)
            {
                var result = queueInstance.Dequeue();
                queueInstance.Acknowledge(result.AckId);
            }
            queueInstance.Purge();
        }
    }
}
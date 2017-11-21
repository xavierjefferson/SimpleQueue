using System;
using System.IO;
using fissolue.SimpleQueue.FluentNHibernate;
using FluentNHibernate.Cfg.Db;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace fissolue.SimpleQueue.FluentNHibernateTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var file = Path.Combine(Environment.CurrentDirectory, Guid.NewGuid() + ".db");
            IPersistenceConfigurer pc = SQLiteConfiguration.Standard.UsingFile(file);
            var queueInstance = new QueueInstance<int>("test2", pc, true,
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
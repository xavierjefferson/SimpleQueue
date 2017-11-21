using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using fissolue.SimpleQueue.FluentNHibernate;
using FluentNHibernate.Cfg.Db;

namespace fissolue.SimpleQueue.Blaster
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var file = Path.Combine(Environment.CurrentDirectory, "e2c49f8c-1dd0-45ac-8393-95c12dac4e4b.db");
            IPersistenceConfigurer pc = SQLiteConfiguration.Standard.UsingFile(file);
            //IPersistenceConfigurer pc = MsSqlConfiguration.MsSql2008.ConnectionString(@"Data Source=.\SQLEXPRESS;database=queuetest;Integrated Security=True");
            var queueInstance = new QueueInstance<int>("test2", pc, true,
                new LocalOptions<int> {SerializationType = SerializationTypeEnum.BinaryFormatter});
            for (var x = 0; x < 100; x++)
            {
                var bw = new BackgroundWorker();
                var x1 = x;
                bw.DoWork += (a, b) =>
                {
                    var queueInstance1 = new QueueInstance<int>("test2", pc, true,
                        new LocalOptions<int> {SerializationType = SerializationTypeEnum.DataContractJsonSerializer});
                    while (true)
                    {
                        var m = queueInstance1.Dequeue();
                        if (m == null)
                        {
                            Thread.Sleep(20);
                        }
                        else
                        {
                            Console.WriteLine("{0} dequeued {1}", x1, m.AckId);
                            queueInstance1.Acknowledge(m.AckId);
                        }
                    }
                };
                bw.RunWorkerAsync();
            }
            //Console.ReadLine();
            var rx = new Random();
            while (true)
            {
                if (rx.NextDouble() < .0001)
                {
                    queueInstance.Purge();
                }
                queueInstance.Enqueue(rx.Next());
                Thread.Sleep(rx.Next(1, 20));
            }

            queueInstance.Purge();
        }
    }
}
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using fissolue.SimpleQueue.FluentNHibernate;
using FluentNHibernate.Cfg.Db;
using Snork.FluentNHibernateTools;

namespace fissolue.SimpleQueue.Blaster
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Func<ISimpleQueue<int>> queueFunc = null;
            while (queueFunc == null)
            {
                Console.WriteLine("Options");
                Console.WriteLine("Enter 1 to use sqlite/NHibernate");

                Console.WriteLine("Enter 3 to use Mysql/NHIbernate");
                Console.WriteLine("Press Enter to exit");
                var readLine = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(readLine))
                {
                    return;
                }
                var file = Path.Combine(Environment.CurrentDirectory, "e2c49f8c-1dd0-45ac-8393-95c12dac4e4b.db");

                switch (readLine)
                {
                    case "1":
                        queueFunc = () =>
                        {
                            IPersistenceConfigurer pc = SQLiteConfiguration.Standard.UsingFile(file);

                            return new FluentNHibernateQueue<int>("test2", pc, true,
                                new LocalOptions<int> {SerializationType = SerializationTypeEnum.BinaryFormatter});
                        };
                        break;
                    case "3":
                        queueFunc = () =>
                        {
                            return new FluentNHibernateQueue<int>("test2", ProviderTypeEnum.MySQL,
                                "Server=localhost;Database=simplequeue;Uid=simplequeue;Pwd=password;",
                                new LocalOptions<int> {SerializationType = SerializationTypeEnum.BinaryFormatter});
                        };
                        break;
                }
            }


            for (var x = 0; x < 5; x++)
            {
                var bw = new BackgroundWorker();
                var index = x;
                bw.DoWork += (a, b) =>
                {
                    var queueInstance1 = queueFunc();
                    while (true)
                    {
                        var m = queueInstance1.Dequeue();
                        if (m == null)
                        {
                            Thread.Sleep(20);
                        }
                        else
                        {
                            Console.WriteLine("Worker {0} dequeued value {2} with ack id {1}", index, m.AckId,
                                m.Data.ToString().PadLeft(12));
                            queueInstance1.Acknowledge(m.AckId);
                        }
                    }
                };
                bw.RunWorkerAsync();
            }
            //Console.ReadLine();
            var rx = new Random();
            var insertingQueue = queueFunc();
            while (true)
            {
                if (rx.NextDouble() < .0001)
                {
                    Console.WriteLine("Purging...");
                    insertingQueue.Purge();
                }
                insertingQueue.Enqueue(rx.Next());
                Thread.Sleep(rx.Next(1, 5) * 100);
            }

            insertingQueue.Purge();
        }
    }
}
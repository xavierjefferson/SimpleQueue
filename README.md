SimpleQueue - a simple database-backed queue implementation for .NET
========================================


Release Notes
-------------




Features
--------
SimpleQueue is a [NuGet library](https://www.nuget.org/packages/fissolue.SimpleQueue/) that you can add in to your project that will allow you to persist objects into one or more queues.  The current implementation is based on Fluent NHibernate, so you can use this implementation on top of all the databases NHibernate currently supports (MS SQL Server 2005 and above, Oracle, Microsoft Access, Firebird, PostgreSQL, DB2 UDB, MySQL, SQLite).  An Entity Framework version is forthcoming.

You can decide the best way to persist the data - all have advantages and disadvantages.  You can use .NET's binary serialization, JSON.net serialization, the DataContractJsonSerializer, or XML.  For binary serialization, any complex objects must be tagged with the [Serializable] attribute.

Once initialized, there are four core methods:

(1) Enqueue an object
(2) Dequeue an object (note that any dequeued object may be dequeued again if you fail to call the Acknowledge method after using it)
(3) Acknowledge - this means you've processed an dequeued item so it is no longer available.
(4) Purge.  This removes ALL acknowledged items from persistence and should only be called periodically to clean up the database tables.

Example usage:

```csharp
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using fissolue.SimpleQueue.EntityFramework;
using fissolue.SimpleQueue.FluentNHibernate;
using FluentNHibernate.Cfg.Db;

public class Program
{
    private static void Main(string[] args)
    {
	    IPersistenceConfigurer configurer = MsSqlConfiguration.MsSql2012.ConnectionString("Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;");
		
		//this creates a queue that stores <int> data type, but you could replace it with anything that will serialize. 
		var myQueue = new FluentNHibernateQueue<int>("dummyQueue", configurer, true, new LocalOptions<int> {SerializationType = SerializationTypeEnum.NewtonsoftJson});
		
		//ok, enqueue some items
		for(var i = 0; i<10; i++){
			myQueue.Enqueue(i);
		}
		
		//now, de-queue them
		while(true)
		{
		    var dequeuedItem = myQueue.Dequeue();
			if (dequeuedItem == null)
			{
				//queue is empty
				break;
			}
			Console.WriteLine(dequeuedItem.Data); //Data property contains the values
			
			//do more processing here with whatever you pulled out of the queue
			
			//now let the db know you're done with the item.
			myQueue.Acknowledge(dequeuedItem.AckId);
		}
		//Clean up
		myQueue.Purge();
	}
}            
```

By default, a dequeued item will revert back to enqueued if you don't call the Acknowledge method for it within 30 seconds.  To get around this, you can (a) pass a timespan of your choosing as an argument to the Dequeue method, or (b) in the LocalOptions object used to initialize the queue, use the Visibility property to set a default timespan.  This mechanism is here essentially so if your process dies, the item stays in the queue.

Sharable on Multiple Clients
----------------------------
Because this is database-backed, you can point multiple processes to it, even if they run on different machines.  This implementation intentionally does NOT use database transactions, because we want to avoid deadlock situations.

Code-First Approach
-------------------
A key feature of SimpleQueue is the fact that you don't even need to bother creating tables in the database.  It uses only TWO tables, and you can have as many queues (uniquely named) as you like.  In your database of choice, you must have table creation permissions if the SimpleQueue tables don't exist already.

RDMBS Support
-------------
Fluent NHibernate supports MS SQL Server with no additional libraries required.  For the other databases, your application must include the 'standard' data provider package (from Nuget or otherwise) for the database provider (i.e. MySQL.Data, System.Data.SQLite).  Whatever the case, you'll just pass an instance of IPersistenceConfigurer so SimpleQueue knows where to put its data.  More info: https://github.com/jagregory/fluent-nhibernate/wiki/Database-configuration

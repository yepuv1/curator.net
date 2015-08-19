using System;
using ZooKeeperNet;
using System.Threading;


namespace CuratorClient
{
	
	public class HandleHolder
	{
		
		private  static IZookeeperFactory zookeeperFactory;
		private  static IWatcher watcher;
		private  static IEnsembleProvider ensembleProvider;
		private  static TimeSpan sessionTimeout;
		private  static bool canBeReadOnly;
		private  static IHelper helper = null;

		public HandleHolder(IZookeeperFactory zookeeperFactory, IWatcher watcher, IEnsembleProvider ensembleProvider, TimeSpan sessionTimeout, bool canBeReadOnly)
		{
			zookeeperFactory = zookeeperFactory;
			watcher = watcher;
			ensembleProvider = ensembleProvider;
			sessionTimeout = sessionTimeout;
			canBeReadOnly = canBeReadOnly;
			helper = new Helper ();
		}

		public IZooKeeper GetZooKeeper() 
		{
			return (helper != null) ? helper.GetZooKeeper() : null;
		}

		public String  GetConnectionString()
		{
			return (helper != null) ? helper.GetConnectionString() : null;
		}

		public bool HasNewConnectionString() 
		{
			String helperConnectionString = (helper != null) ? helper.GetConnectionString() : null;
			return (helperConnectionString != null) && !ensembleProvider.GetConnectionString().Equals(helperConnectionString);
		}

		public void CloseAndClear()
		{
			InternalClose();
			helper = null;
		}

		public void CloseAndReset()
		{
			InternalClose();

			// first helper is synchronized when getZooKeeper is called. Subsequent calls
			// are not synchronized.
			helper = new Helper();
		}

		private void InternalClose() 
		{
			try
			{
				IZooKeeper zooKeeper = (helper != null) ? helper.GetZooKeeper() : null;
				if ( zooKeeper != null )
				{
					IWatcher dummyWatcher = new DummyWatcher();
					zooKeeper.Register(dummyWatcher);   // clear the default watcher so that no new events get processed by mistake
					zooKeeper.Dispose();
				}
			}
			catch ( ThreadInterruptedException dummy)
			{
				Thread.CurrentThread.Interrupt ();

			}
		}

		[System.Runtime.Remoting.Contexts.Synchronization]
		public class Helper : IHelper
		{
			private volatile IZooKeeper zooKeeperHandle = null;
			private volatile String connectionString = null;

			public IZooKeeper GetZooKeeper() 
			{

				if ( zooKeeperHandle == null )
				{
					connectionString = ensembleProvider.GetConnectionString();
					zooKeeperHandle = zookeeperFactory.NewZooKeeper(connectionString, sessionTimeout, watcher, canBeReadOnly);
				}

				return zooKeeperHandle;

			}

			public String GetConnectionString()
			{
				return connectionString;
			}
		}
	}

}


using System;
using ZooKeeperNet;
using System.Threading;


namespace CuratorClient
{
	
	public class HandleHolder
	{
		
		private  readonly IZookeeperFactory zookeeperFactory;
		private  readonly IWatcher watcher;
		private  readonly IEnsembleProvider ensembleProvider;
		private  readonly TimeSpan sessionTimeout;
		private  readonly bool canBeReadOnly;
		private  volatile IHelper helper = null;

		public HandleHolder(IZookeeperFactory zookeeperFactory, IWatcher watcher, IEnsembleProvider ensembleProvider, TimeSpan sessionTimeout, bool canBeReadOnly)
		{
			this.zookeeperFactory = zookeeperFactory;
            this.watcher = watcher;
            this.ensembleProvider = ensembleProvider;
            this.sessionTimeout = sessionTimeout;
            this.canBeReadOnly = canBeReadOnly;
            this.helper = null;
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
			helper = new SyncHelper(this);
		}

		private void InternalClose() 
		{
			try
			{
				IZooKeeper zooKeeper = (helper != null) ? helper.GetZooKeeper() : null;
				if ( zooKeeper != null )
				{
					IWatcher dummyWatcher = new CuratorWatcher();
					zooKeeper.Register(dummyWatcher);   // clear the default watcher so that no new events get processed by mistake
					zooKeeper.Dispose();
				}
			}
			catch ( ThreadInterruptedException dummy)
			{
				Thread.CurrentThread.Interrupt ();

			}
		}

		
		private class SyncHelper : IHelper
		{
			private volatile IZooKeeper zooKeeperHandle = null;
			private volatile String connectionString = null;
            private HandleHolder parent;

            public SyncHelper(HandleHolder parent)
            {
                this.parent = parent;
            }

			public IZooKeeper GetZooKeeper() 
			{
                lock (this)
                {

                    if (zooKeeperHandle == null)
                    {
                        connectionString = parent.ensembleProvider.GetConnectionString();
                        zooKeeperHandle =  parent.zookeeperFactory.NewZooKeeper(connectionString, parent.sessionTimeout, parent.watcher, parent.canBeReadOnly);
                    }

                    parent.helper = new SyncHelper.Helper(this);
                    return zooKeeperHandle;
                }
			}

			public String GetConnectionString()
			{
				return connectionString;
			}

            class Helper : IHelper
            {
                private volatile IZooKeeper zooKeeperHandle = null;
                private volatile String connectionString = null;
                private SyncHelper parent;

                public Helper(SyncHelper parent)
                {
                    this.parent = parent;
                    zooKeeperHandle = parent.zooKeeperHandle;
                    connectionString = parent.connectionString;
                }

                public IZooKeeper GetZooKeeper()
                {
                    return parent.zooKeeperHandle;
                   
                }

                public String GetConnectionString()
                {
                    return parent.connectionString;
                }
            }
        }

      
    }

}


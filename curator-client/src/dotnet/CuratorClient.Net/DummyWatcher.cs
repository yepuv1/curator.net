using System;
using ZooKeeperNet;

namespace CuratorClient
{
	public class DummyWatcher: IWatcher
	{
		public DummyWatcher ()
		{
		}

		public void Process(WatchedEvent @event){


		}
	}
}


using System;
using ZooKeeperNet;

namespace CuratorClient
{
	public class CuratorWatcher: IWatcher
	{
		Action<WatchedEvent>  action;
		public CuratorWatcher ()
		{
		}

		public CuratorWatcher (Action<WatchedEvent> e)
		{
			action = e;
		}

		public void Process(WatchedEvent @event){

			if (action != null) {
				action (@event);
			}
		}
	}
}


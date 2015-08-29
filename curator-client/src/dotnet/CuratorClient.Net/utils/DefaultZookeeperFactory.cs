using System;
using ZooKeeperNet;

namespace CuratorClient
{
	public class DefaultZookeeperFactory : IZookeeperFactory
	{
		
		public IZooKeeper NewZooKeeper(String connectString, TimeSpan sessionTimeout, IWatcher watcher, bool canBeReadOnly) 
		{

			return new ZooKeeper(connectString, sessionTimeout, watcher);
		}
	}
}


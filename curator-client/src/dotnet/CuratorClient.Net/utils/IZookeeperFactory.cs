using System;
using ZooKeeperNet;

namespace CuratorClient
{
	public interface IZookeeperFactory
	{
		/**
     * Allocate a new ZooKeeper instance
     *
     *
     * @param connectString the connection string
     * @param sessionTimeout session timeout in milliseconds
     * @param watcher optional watcher
     * @param canBeReadOnly if true, allow ZooKeeper client to enter
     *                      read only mode in case of a network partition. See
     *                      {@link ZooKeeper#ZooKeeper(String, int, Watcher, long, byte[], bool)}
     *                      for details
     * @return the instance
     * @throws Exception errors
     */
		IZooKeeper NewZooKeeper(String connectString, TimeSpan sessionTimeout, IWatcher watcher, bool canBeReadOnly);
	}

}


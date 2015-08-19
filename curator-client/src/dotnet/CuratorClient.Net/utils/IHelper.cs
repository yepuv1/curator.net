using System;
using ZooKeeperNet;

namespace CuratorClient
{
	public interface IHelper
	{
		IZooKeeper GetZooKeeper() ;

		String GetConnectionString();
	}
}


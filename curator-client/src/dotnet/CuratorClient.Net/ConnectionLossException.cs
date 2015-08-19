using System;
using ZooKeeperNet;

namespace CuratorClient
{
	
	public class CuratorConnectionLossException : KeeperException.ConnectionLossException
	{
		private const long serialVersionUID = 1L;
	}
}


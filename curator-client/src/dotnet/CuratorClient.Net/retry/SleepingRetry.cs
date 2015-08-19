using System;
using ZooKeeperNet;

namespace CuratorClient
{
	public abstract class SleepingRetry: IRetryPolicy
	{
		private readonly int n;

		public SleepingRetry(int n)
		{
			this.n = n;
		}
			
		public int N
		{
			get{
				return n;
			}
		}

		public bool AllowRetry(int retryCount, TimeSpan elapsedTime, Action<TimeSpan> sleeper)
		{
			if ( retryCount < n )
			{
				try
				{
					
					sleeper(elapsedTime);
				}
				catch ( Exception e )
				{
					//Thread.currentThread().interrupt();
					return false;
				}
				return true;
			}
			return false;
		}

		protected abstract TimeSpan GetSleepTime(int retryCount, TimeSpan elapsedTime);
	}
}


using System;

namespace CuratorClient
{
	/**
 * Retry policy that retries a max number of times
 */
	public class RetryNTimes: SleepingRetry
	{
		private readonly TimeSpan sleepBetweenRetries;


		public RetryNTimes(int n, TimeSpan sleepBetweenRetries):base(n)
		{
			
			this.sleepBetweenRetries = sleepBetweenRetries;
		}


		override protected TimeSpan GetSleepTime(int retryCount, TimeSpan elapsedTime)
		{
			return sleepBetweenRetries;
		}
	}

}


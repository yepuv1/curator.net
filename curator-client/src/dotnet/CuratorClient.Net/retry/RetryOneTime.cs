using System;

namespace CuratorClient
{
	/**
 * A retry policy that retries only once
 */
	public class RetryOneTime : RetryNTimes
	{
		public RetryOneTime(TimeSpan sleepBetweenRetry):base(2, sleepBetweenRetry)
		{
		}
	}
}


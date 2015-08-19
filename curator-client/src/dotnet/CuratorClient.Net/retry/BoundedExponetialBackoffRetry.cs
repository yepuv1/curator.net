using System;

namespace CuratorClient
{
	/**
 * Retry policy that retries a set number of times with an increasing (up to a maximum bound) sleep time between retries
 */
	public class BoundedExponentialBackoffRetry : ExponentialBackoffRetry
	{
		private  TimeSpan maxSleepTime;

		/**
     * @param baseSleepTimeMs initial amount of time to wait between retries
     * @param maxSleepTimeMs maximum amount of time to wait between retries
     * @param maxRetries maximum number of times to retry
     */
		public BoundedExponentialBackoffRetry(TimeSpan baseSleepTime, TimeSpan maxSleepTime, int maxRetries):base(baseSleepTime,maxRetries)
		{
			this.maxSleepTime = maxSleepTime;
		}


		public TimeSpan GetMaxSleepTime()
		{
			return maxSleepTime;
		}


		override protected TimeSpan GetSleepTime(int retryCount, TimeSpan elapsedTime)
		{
			return new TimeSpan(Math.Min(maxSleepTime.Ticks, base.GetSleepTime(retryCount, elapsedTime).Ticks));
		}
	}
}


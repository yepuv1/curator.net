using System;

namespace CuratorClient
{
	/**
 * Retry policy that retries a set number of times with increasing sleep time between retries
 */
	public class ExponentialBackoffRetry:  SleepingRetry
	{
		//private static final Logger     log = LoggerFactory.getLogger(ExponentialBackoffRetry.class);

		private const int MAX_RETRIES_LIMIT = 29;
		private static TimeSpan DEFAULT_MAX_SLEEP = new TimeSpan(long.MaxValue);

		private readonly Random random = new Random();
		private readonly TimeSpan baseSleepTime;
		private readonly TimeSpan maxSleep;

		/**
     * @param baseSleepTimeMs initial amount of time to wait between retries
     * @param maxRetries max number of times to retry
     */
		public ExponentialBackoffRetry(TimeSpan baseSleepTime, int maxRetries):this(baseSleepTime, maxRetries, DEFAULT_MAX_SLEEP)
		{

		}

		/**
     * @param baseSleepTimeMs initial amount of time to wait between retries
     * @param maxRetries max number of times to retry
     * @param maxSleepMs max time in ms to sleep on each retry
     */
		public ExponentialBackoffRetry(TimeSpan baseSleepTime, int maxRetries, TimeSpan maxSleep):base(ValidateMaxRetries(maxRetries))
		{
			this.baseSleepTime = baseSleepTime;
			this.maxSleep = maxSleep;
		}
			
		public TimeSpan GetBaseSleepTime()
		{
			return baseSleepTime;
		}
			
		override protected TimeSpan GetSleepTime(int retryCount, TimeSpan elapsedTime)
		{
			// copied from Hadoop's RetryPolicies.java

			long sleep = baseSleepTime.Ticks * Math.Max(1, random.Next(1 << (retryCount + 1)));
			if ( sleep > maxSleep.Ticks )
			{
				//log.warn(String.format("Sleep extension too large (%d). Pinning to %d", sleepMs, maxSleepMs));
				sleep = maxSleep.Ticks;
			}
			return new TimeSpan(sleep);
		}

		private static int ValidateMaxRetries(int maxRetries)
		{
			if ( maxRetries > MAX_RETRIES_LIMIT )
			{
				//log.warn(String.format("maxRetries too large (%d). Pinning to %d", maxRetries, MAX_RETRIES_LIMIT));
				maxRetries = MAX_RETRIES_LIMIT;
			}
			return maxRetries;
		}
	}

}


using System;

namespace CuratorClient
{
	/**
 * A retry policy that retries until a given amount of time elapses
 */
	public class RetryUntilElapsed : SleepingRetry
	{
		private readonly TimeSpan maxElapsedTime;
		private readonly TimeSpan sleepBetweenRetries;

		public RetryUntilElapsed(TimeSpan maxElapsedTime, TimeSpan sleepBetweenRetries):base(Int16.MaxValue)
		{
			this.maxElapsedTime = maxElapsedTime;
			this.sleepBetweenRetries = sleepBetweenRetries;
		}


		new public bool AllowRetry(int retryCount, TimeSpan elapsedTime, Action<TimeSpan> sleeper)
		{
			return base.AllowRetry(retryCount, elapsedTime, sleeper) && (elapsedTime.Ticks < maxElapsedTime.Ticks);
		}


		override protected TimeSpan GetSleepTime(int retryCount, TimeSpan elapsedTimeM)
		{
			return sleepBetweenRetries;
		}
	}
}


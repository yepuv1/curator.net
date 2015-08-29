using System;

namespace CuratorClient
{
	public interface IRetryPolicy
	{
		bool AllowRetry(int retryCount, TimeSpan elapsedTime, Action<TimeSpan> sleeper);
	}
}


using System;

namespace CuratorClient
{
	
	/// <summary>
	/// Abstracts the policy to use when retrying connections
	/// </summary>
	public interface IRetryPolicy
	{

		/// <summary>
		/// Allows the retry.
		/// </summary>
		/// <returns>true/false</returns>
		/// <param name="retryCount">the number of times retried so far (0 the first time)</param>
		/// <param name="elapsedTime">the elapsed time since the operation was attempted</param>
		/// <param name="sleeper">sleeper use this to sleep - DO NOT call Thread.sleep</param>
		/// 
		bool AllowRetry(int retryCount, TimeSpan elapsedTime, IRetrySleeper sleeper);

	}
}


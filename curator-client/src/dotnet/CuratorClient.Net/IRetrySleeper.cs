using System;

namespace CuratorClient
{
	/// <summary>
	/// Sleep for the given time
	/// </summary>
	public interface IRetrySleeper
	{
		/// <summary>
		/// Sleep for the given time
		/// </summary>
		/// <param name="time">Time</param>
		/// <param name="unit">time unit.</param>
		void	SleepFor(TimeSpan time);
	}
}


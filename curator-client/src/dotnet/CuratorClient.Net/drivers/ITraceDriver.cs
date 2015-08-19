using System;

namespace CuratorClient
{

	/// <summary>
	/// Mechanism for timing methods and recording counters
	/// </summary>
	public interface ITracerDriver
	{
		/// <summary>
		/// Record the given trace event
		/// </summary>
		/// <param name="name">name of the event</param>
		/// <param name="time">time event took</param>
		void     AddTrace(String name, TimeSpan time);

		/// <summary>
		/// Add to a named counter
		/// </summary>
		/// <param name="name">name of the counter</param>
		/// <param name="increment">amount to increment</param>
		void     AddCount(String name, int increment);
	}
}


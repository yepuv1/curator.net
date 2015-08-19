using System;

namespace CuratorClient
{

	/// <summary>
	/// Utility to time a method or portion of code
	/// </summary>
	public class TimeTrace
	{
		private readonly String name;
		private readonly ITracerDriver driver;
		private readonly long startTimeTicks = System.DateTime.Now.Ticks;


		/// <summary>
		/// Create and start a timer <see cref="CuratorClient.TimeTrace"/> class.
		/// </summary>
		/// <param name="name">name of the event.</param>
		/// <param name="driver">driver.</param>
		public TimeTrace(String name, ITracerDriver driver)
		{
			this.name = name;
			this.driver = driver;
		}
			
		/// <summary>
		/// Record the elapsed time
		/// </summary>
		public void Commit()
		{
			long	elapsed = System.DateTime.Now.Ticks - startTimeTicks;
			driver.AddTrace(name, new TimeSpan(elapsed));
		}
	}
}


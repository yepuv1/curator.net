using System;
using log4net;

namespace CuratorClient
{
	/**
 * Default tracer driver
 */
	public class DefaultTracerDriver: ITracerDriver
	{
		private  static ILog log = LogManager.GetLogger(typeof(DefaultTracerDriver));


		public void AddTrace(String name, TimeSpan time)
		{
			
			log.Debug("Trace: " + name + " - " + time.TotalMilliseconds + " ms");

		}


		public void AddCount(String name, int increment)
		{
			
			log.Debug("Counter " + name + ": " + increment);

		}
	}
}


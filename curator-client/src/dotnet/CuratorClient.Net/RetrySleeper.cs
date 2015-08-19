using System;
using System.Threading;

namespace CuratorClient
{
	public class RetrySleeper: IRetrySleeper
	{
		public RetrySleeper ()
		{
		}

		public void SleepFor(TimeSpan time)
		{
			Thread.Sleep ( (int)time.TotalMilliseconds);
		}
	}
}


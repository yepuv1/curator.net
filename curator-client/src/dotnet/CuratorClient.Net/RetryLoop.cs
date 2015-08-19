using System;
using System.Threading;
using log4net;
using ZooKeeperNet;

namespace CuratorClient
{
		/**
	 * <p>Mechanism to perform an operation on Zookeeper that is safe against
	 * disconnections and "recoverable" errors.</p>
	 *
	 * <p>
	 * If an exception occurs during the operation, the RetryLoop will process it,
	 * check with the current retry policy and either attempt to reconnect or re-throw
	 * the exception
	 * </p>
	 *
	 * Canonical usage:<br>
	 * <pre>
	 * RetryLoop retryLoop = client.newRetryLoop();
	 * while ( retryLoop.shouldContinue() )
	 * {
	 *     try
	 *     {
	 *         // do your work
		*         ZooKeeper      zk = client.getZooKeeper();    // it's important to re-get the ZK instance in case there was an error and the instance was re-created
		*
		*         retryLoop.markComplete();
		*     }
	*     catch ( Exception e )
	*     {
		*         retryLoop.takeException(e);
		*     }
	* }
	* </pre>
	*/
	public class RetryLoop
	{
		private bool         isDone = false;
		private int             retryCount = 0;

		private static readonly ILog logger = LogManager.GetLogger(typeof(RetryLoop));
		private long			startTimeTicks = System.DateTime.Now.Ticks;
		private IRetryPolicy 	retryPolicy;
		private ITracerDriver	tracer;
		private static IRetrySleeper	sleeper = new RetrySleeper ();


		/**
	     * Returns the default retry sleeper
	     *
	     * @return sleeper
	     */
		public static IRetrySleeper RetrySleeper
		{
			get
			{
				return sleeper;
			}
		}

		/**
	     * Convenience utility: creates a retry loop calling the given proc and retrying if needed
	     *
	     * @param client Zookeeper
	     * @param proc procedure to call with retry
	     * @param <T> return type
	     * @return procedure result
	     * @throws Exception any non-retriable errors
	     */
		public static T CallWithRetry<T>(CuratorZookeeperClient client, Func<T> proc) 
		{
		T               result = default(T);
			RetryLoop       retryLoop = client.NewRetryLoop();

			while (retryLoop.ShouldContinue())
			{
				try
				{
					client.InternalBlockUntilConnectedOrTimedOut();
					
					result = proc();
					retryLoop.MarkComplete();
				}
				catch ( Exception e )
				{
					retryLoop.TakeException(e);
				}
			}
			return result;
		}

		public RetryLoop(IRetryPolicy retryPolicy, ITracerDriver tracer)
		{
			this.retryPolicy = retryPolicy;
			this.tracer = tracer;
		}

		/**
	     * If true is returned, make an attempt at the operation
	     *
	     * @return true/false
	     */
		public bool      ShouldContinue()
		{
			return !isDone;
		}

		/**
	     * Call this when your operation has successfully completed
	     */
		public void     MarkComplete()
		{
			isDone = true;
		}

		/**
	     * Utility - return true if the given Zookeeper result code is retry-able
	     *
	     * @param rc result code
	     * @return true/false
	     */
		public static bool      ShouldRetry(int rc)
		{
		return (rc == (int) KeeperException.Code.CONNECTIONLOSS) ||
			(rc == (int) KeeperException.Code.OPERATIONTIMEOUT) ||
			(rc == (int)KeeperException.Code.SESSIONMOVED) ||
			(rc == (int) KeeperException.Code.SESSIONEXPIRED);
		}

		/**
	     * Utility - return true if the given exception is retry-able
	     *
	     * @param exception exception to check
	     * @return true/false
	     */
		public static bool      IsRetryException(Exception exception)
		{
			if ( exception.GetType() == typeof( KeeperException ))
			{
				KeeperException     keeperException = (KeeperException)exception;
			return ShouldRetry((int)keeperException.ErrorCode);
			}
			return false;
		}

		/**
	     * Pass any caught exceptions here
	     *
	     * @param exception the exception
	     * @throws Exception if not retry-able or the retry policy returned negative
	     */
		public void         TakeException(Exception exception) 
		{
			bool     rethrow = true;
			if ( IsRetryException(exception) )
			{
			if ( retryPolicy.AllowRetry(retryCount++, TimeSpan.FromTicks(System.DateTime.Now.Ticks - startTimeTicks), sleeper.SleepFor) )
				{
					tracer.AddCount("retries-allowed", 1);

					rethrow = false;
				}
				else
				{
					tracer.AddCount("retries-disallowed", 1);

				}
			}

			if ( rethrow )
			{
				throw exception;
			}
		}
	}

}


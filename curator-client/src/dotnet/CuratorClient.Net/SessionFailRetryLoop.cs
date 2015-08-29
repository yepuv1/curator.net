using System;
using ZooKeeperNet;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace CuratorClient
{
	/**
 * <p>
 *     See {@link RetryLoop} for the main details on retry loops. <b>All Curator/ZooKeeper operations
 *     should be done in a retry loop.</b>
 * </p>
 *
 * <p>
 *     The standard retry loop treats session failure as a type of connection failure. i.e. the fact
 *     that it is a session failure isn't considered. This can be problematic if you are performing
 *     a series of operations that rely on ephemeral nodes. If the session fails after the ephemeral
 *     node has been created, future Curator/ZooKeeper operations may succeed even though the
 *     ephemeral node has been removed by ZooKeeper.
 * </p>
 *
 * <p>
 *     Here's an example:
 * </p>
 *     <ul>
 *         <li>You create an ephemeral/sequential node as a kind of lock/marker</li>
 *         <li>You perform some other operations</li>
 *         <li>The session fails for some reason</li>
 *         <li>You attempt to create a node assuming that the lock/marker still exists
 *         <ul>
 *             <li>Curator will notice the session failure and try to reconnect</li>
 *             <li>In most cases, the reconnect will succeed and, thus, the node creation will succeed
 *             even though the ephemeral node will have been deleted by ZooKeeper.</li>
 *         </ul>
 *         </li>
 *     </ul>
 *
 * <p>
 *     The SessionFailRetryLoop prevents this type of scenario. When a session failure is detected,
 *     the thread is marked as failed which will cause all future Curator operations to fail. The
 *     SessionFailRetryLoop will then either retry the entire
 *     set of operations or fail (depending on {@link SessionFailRetryLoop.Mode})
 * </p>
 *
 * Canonical usage:<br>
 * <pre>
 * SessionFailRetryLoop    retryLoop = client.newSessionFailRetryLoop(mode);
 * retryLoop.start();
 * try
 * {
 *     while ( retryLoop.shouldContinue() )
 *     {
 *         try
 *         {
 *             // do work
	*         }
*         catch ( Exception e )
*         {
	*             retryLoop.takeException(e);
	*         }
*     }
* }
* finally
* {
	*     retryLoop.close();
	* }
* </pre>
*/
public class SessionFailRetryLoop : IDisposable 
{
	public enum Mode
	{
		/**
         * If the session fails, retry the entire set of operations when {@link SessionFailRetryLoop#shouldContinue()}
         * is called
         */
		RETRY,

		/**
         * If the session fails, throw {@link KeeperException.SessionExpiredException} when
         * {@link SessionFailRetryLoop#shouldContinue()} is called
         */
		FAIL
	}

	public class  tempWatcher<T>: IWatcher
	{
        Func<T> func;
        public tempWatcher(Func<T> func){
        this.func = func;
        }

		public void Process(WatchedEvent @event)
		{
			if ( @event.State == KeeperState.Expired )
			{
                    func();
			}
		}
	};


	public class SessionFailedException : Exception
	{
		private static long serialVersionUID = 1L;
	}


	private  readonly CuratorZookeeperClient client;
	private  readonly SessionFailRetryLoop.Mode  mode;
	private  readonly Thread ourThread = Thread.CurrentThread;
	private  bool   sessionHasFailed = false;
	private  bool   isDone = false;
	private  readonly RetryLoop  retryLoop;
	private  static readonly ConcurrentDictionary<Thread,bool> failedSessionThreads = new ConcurrentDictionary<Thread, bool>();
    private IWatcher watcher;


    /**
     * Convenience utility: creates a "session fail" retry loop calling the given proc
     *
     * @param client Zookeeper
     * @param mode how to handle session failures
     * @param proc procedure to call with retry
     * @param <T> return type
     * @return procedure result
     * @throws Exception any non-retriable errors
     */

    public static T CallWithRetry<T>(CuratorZookeeperClient client, Mode mode, Func<T> proc) 
	{
		T               result = default(T);
		SessionFailRetryLoop    retryLoop = client.NewSessionFailRetryLoop(mode);
		retryLoop.Start();
		try{
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
		}
		finally{

			retryLoop.Dispose();
		}
		return result;
	}


	public SessionFailRetryLoop(CuratorZookeeperClient client, Mode _mode)
	{
        watcher = new tempWatcher<bool>(() => {

            sessionHasFailed = true;
            failedSessionThreads.GetOrAdd(ourThread, false);
            return true;
        });


        this.client = client;
		mode = _mode;
		retryLoop = client.NewRetryLoop();
	}

	public static bool SessionForThreadHasFailed()
	{
		return (failedSessionThreads.Count > 0) && failedSessionThreads.ContainsKey(Thread.CurrentThread);
	}

	/**
     * SessionFailRetryLoop must be started
     */
	public void Start()
	{
		Contract.Ensures(Thread.CurrentThread.Equals(ourThread), "Not in the correct thread");
		//Preconditions.checkState(Thread.CurrentThread.Equals(ourThread), "Not in the correct thread");

		client.AddParentWatcher(watcher);
	}

	/**
     * Call this when your operation has successfully completed
     */
	public void MarkComplete()
	{
		isDone = true;
	}
	/**
     * If true is returned, make an attempt at the set of operations
     *
     * @return true/false
     */
	public bool ShouldContinue()
	{
			isDone = true;
			
			bool localIsDone = isDone;
		return !localIsDone;
	}

	/**
     * Must be called in a finally handler when done with the loop
     */

	public void Dispose()
	{
		Contract.Ensures( Thread.CurrentThread.Equals(ourThread),"Not in the correct thread");
        bool tmpValue;
		failedSessionThreads.TryRemove (ourThread, out tmpValue);
		client.RemoveParentWatcher(watcher);
	}

	/**
     * Pass any caught exceptions here
     *
     * @param exception the exception
     * @throws Exception if not retry-able or the retry policy returned negative
     */
	public void TakeException(Exception exception)
	{
		Contract.Ensures( Thread.CurrentThread.Equals(ourThread),"Not in the correct thread");

		bool     passUp = true;
		if ( sessionHasFailed )
		{
			switch ( mode )
			{
			case Mode.RETRY:
				{
					sessionHasFailed = false;
					//Interlocked.Exchange(ref sessionHasFailed, false);
					bool tmpvalue;
					failedSessionThreads.TryRemove (ourThread, out tmpvalue);

					if ( exception is SessionFailedException )
					{
							isDone = false;
						//Interlocked.Exchange (ref isDone, false);
						passUp = false;
					}
					break;
				}

			case Mode.FAIL:
				{
					break;
				}
			}
		}

		if ( passUp )
		{
			retryLoop.TakeException(exception);
		}
	}
}

}


using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using ZooKeeperNet;
using log4net;
using CuratorClient;
using System.IO;
using System.Diagnostics.Contracts;

namespace CuratorClient
{
	/**
 * A wrapper around Zookeeper that takes care of some low-level housekeeping
 */

	public class CuratorZookeeperClient: IDisposable
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(CuratorZookeeperClient));
		private  ConnectionState   state;
		private  IRetryPolicy      retryPolicy =  null;
		private  int               connectionTimeoutMs;
		private  bool              started = false;
		private  ITracerDriver     tracer = new DefaultTracerDriver();

		/**
     *
     * @param connectString list of servers to connect to
     * @param sessionTimeoutMs session timeout
     * @param connectionTimeoutMs connection timeout
     * @param watcher default watcher or null
     * @param retryPolicy the retry policy to use
     */
		public CuratorZookeeperClient(String connectString, int sessionTimeoutMs, int connectionTimeoutMs, IWatcher watcher, IRetryPolicy retryPolicy)
		:this(new DefaultZookeeperFactory(), new FixedEnsembleProvider(connectString), sessionTimeoutMs, connectionTimeoutMs, watcher, retryPolicy, false)
		{
		}

		/**
     * @param ensembleProvider the ensemble provider
     * @param sessionTimeoutMs session timeout
     * @param connectionTimeoutMs connection timeout
     * @param watcher default watcher or null
     * @param retryPolicy the retry policy to use
     */
		public CuratorZookeeperClient(IEnsembleProvider ensembleProvider, int sessionTimeoutMs, int connectionTimeoutMs, IWatcher watcher, IRetryPolicy retryPolicy):
		this(new DefaultZookeeperFactory(), ensembleProvider, sessionTimeoutMs, connectionTimeoutMs, watcher, retryPolicy, false)
		{
			
		}

		/**
     * @param zookeeperFactory factory for creating {@link ZooKeeper} instances
     * @param ensembleProvider the ensemble provider
     * @param sessionTimeoutMs session timeout
     * @param connectionTimeoutMs connection timeout
     * @param watcher default watcher or null
     * @param retryPolicy the retry policy to use
     * @param canBeReadOnly if true, allow ZooKeeper client to enter
     *                      read only mode in case of a network partition. See
     *                      {@link ZooKeeper#ZooKeeper(String, int, Watcher, long, byte[], bool)}
     *                      for details
     */
		public CuratorZookeeperClient(IZookeeperFactory zookeeperFactory, IEnsembleProvider ensembleProvider, int sessionTimeoutMs, int connectionTimeoutMs, IWatcher watcher, IRetryPolicy retryPolicy, bool canBeReadOnly)
		{
			if ( sessionTimeoutMs < connectionTimeoutMs )
			{
				log.Warn(String.Format("session timeout [{0}] is less than connection timeout [{1}]", sessionTimeoutMs, connectionTimeoutMs));
			}


//			retryPolicy = Preconditions.checkNotNull(retryPolicy, "retryPolicy cannot be null");
//			ensembleProvider = Preconditions.checkNotNull(ensembleProvider, "ensembleProvider cannot be null");

			this.connectionTimeoutMs = connectionTimeoutMs;
			state = new ConnectionState(zookeeperFactory, ensembleProvider, TimeSpan.FromMilliseconds(sessionTimeoutMs), TimeSpan.FromMilliseconds(connectionTimeoutMs), watcher, tracer, canBeReadOnly);
			SetRetryPolicy(retryPolicy);
		}

		/**
     * Return the managed ZK instance.
     *
     * @return client the client
     * @throws Exception if the connection timeout has elapsed or an exception occurs in a background process
     */
		public IZooKeeper GetZooKeeper() 
		{
			Contract.Ensures (started, "\"Client is not started\"");
			//Preconditions.checkState(started.get(), "Client is not started");

			return state.GetZooKeeper();
		}

		/**
     * Return a new retry loop. All operations should be performed in a retry loop
     *
     * @return new retry loop
     */
		public RetryLoop NewRetryLoop()
		{
			return new RetryLoop(retryPolicy, tracer);
		}

		/**
     * Return a new "session fail" retry loop. See {@link SessionFailRetryLoop} for details
     * on when to use it.
     *
     * @param mode failure mode
     * @return new retry loop
     */
		public SessionFailRetryLoop NewSessionFailRetryLoop(SessionFailRetryLoop.Mode mode)
		{
			return new SessionFailRetryLoop(this, mode);
		}

		/**
     * Returns true if the client is current connected
     *
     * @return true/false
     */
		public bool IsConnected()
		{
			return state.IsConnected();
		}

		/**
     * This method blocks until the connection to ZK succeeds. Use with caution. The block
     * will timeout after the connection timeout (as passed to the constructor) has elapsed
     *
     * @return true if the connection succeeded, false if not
     * @throws InterruptedException interrupted while waiting
     */
		public bool BlockUntilConnectedOrTimedOut()
		{
			Contract.Ensures (started, "\"Client is not started\"");
			//Preconditions.checkState(started, "Client is not started");

			log.Debug("blockUntilConnectedOrTimedOut() start");
			TimeTrace       trace = StartTracer("blockUntilConnectedOrTimedOut");

			InternalBlockUntilConnectedOrTimedOut();

			trace.Commit();

			bool localIsConnected = state.IsConnected();
			log.Debug("blockUntilConnectedOrTimedOut() end. isConnected: " + localIsConnected);

			return localIsConnected;
		}

		/**
     * Must be called after construction
     *
     * @throws IOException errors
     */
		public void     Start() 
		{
			log.Debug("Starting");

			started = (started == false) ? true : false;
		
			if ( !started )
			{
				ApplicationException ise = new ApplicationException("Already started");
				throw ise;
			}

			state.Start();
		}

		/**
     * Close the client
     */
		public void     Dispose()
		{
			log.Debug("Closing");

			started = false;
			try
			{
				state.Dispose();
			}
			catch ( IOException e )
			{
				log.Error("", e);
			}
		}

		/**
     * Change the retry policy
     *
     * @param policy new policy
     */
		public void     SetRetryPolicy(IRetryPolicy policy)
		{
			//Preconditions.checkNotNull(policy, "policy cannot be null");

			Interlocked.Exchange(ref retryPolicy, policy);
			//retryPolicy.set(policy);
		}

		/**
     * Return the current retry policy
     *
     * @return policy
     */
		public IRetryPolicy GetRetryPolicy()
		{
			return retryPolicy;
		}

		/**
     * Start a new tracer
     * @param name name of the event
     * @return the new tracer ({@link TimeTrace#commit()} must be called)
     */
		public TimeTrace          StartTracer(String name)
		{
			return new TimeTrace(name, tracer);
		}

		/**
     * Return the current tracing driver
     *
     * @return tracing driver
     */
		public ITracerDriver       GetTracerDriver()
		{
			return tracer;
		}

		/**
     * Change the tracing driver
     *
     * @param tracer new tracing driver
     */
		public void               SetTracerDriver(ITracerDriver tracer)
		{
			Interlocked.Exchange (ref this.tracer, tracer);

		}

		/**
     * Returns the current known connection string - not guaranteed to be correct
     * value at any point in the future.
     *
     * @return connection string
     */
		public String             GetCurrentConnectionString()
		{
			return state.GetEnsembleProvider().GetConnectionString();
		}

		/**
     * Return the configured connection timeout
     *
     * @return timeout
     */
		public int GetConnectionTimeoutMs()
		{
			return connectionTimeoutMs;
		}

		/**
     * Every time a new {@link ZooKeeper} instance is allocated, the "instance index"
     * is incremented.
     *
     * @return the current instance index
     */
		public long GetInstanceIndex()
		{
			return state.GetInstanceIndex();
		}

		public void        AddParentWatcher(IWatcher watcher)
		{
			state.AddParentWatcher(watcher);
		}

		public void        RemoveParentWatcher(IWatcher watcher)
		{
			state.RemoveParentWatcher(watcher);
		}


		private  class TempWatcher:IWatcher{

			private System.Threading.CountdownEvent latch;

			public TempWatcher(System.Threading.CountdownEvent latch){

				this.latch = latch;
			}

			public void Process(WatchedEvent @event)
			{
				latch.Signal ();
			}
		}

		public void InternalBlockUntilConnectedOrTimedOut() 
		{
			long            waitTimeMs = connectionTimeoutMs;
			while ( !state.IsConnected() && (waitTimeMs > 0) )
			{
				
				CountdownEvent latch = new CountdownEvent (1);
				IWatcher tempWatcher = new TempWatcher (latch);


				state.AddParentWatcher(tempWatcher);
				long        startTimeMs = (long)TimeSpan.FromTicks(System.DateTime.Now.Ticks).TotalMilliseconds;
				try
				{
					latch.Wait(TimeSpan.FromSeconds(1));
				}
				finally
				{
					state.RemoveParentWatcher(tempWatcher);
				}
				long        elapsed = (long) Math.Max(1, TimeSpan.FromTicks(System.DateTime.Now.Ticks).TotalMilliseconds - startTimeMs);
				waitTimeMs -= elapsed;
			}
		}
	}

}


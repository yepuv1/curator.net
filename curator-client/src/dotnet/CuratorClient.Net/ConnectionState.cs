using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using ZooKeeperNet;
using log4net;
using CuratorClient;

namespace CuratorClient
{
	[Synchronization]
	public class ConnectionState : IWatcher, IDisposable
	{
		
		private const int MAX_BACKGROUND_EXCEPTIONS = 10;
		private const bool LOG_EVENTS = true;
		private static readonly ILog log = LogManager.GetLogger(typeof(ConnectionState));
		private readonly HandleHolder zooKeeper;
		private bool isConnected = false;
		private IEnsembleProvider ensembleProvider;
		private TimeSpan sessionTimeout;
		private TimeSpan connectionTimeout;
		private ITracerDriver tracer;
		private ConcurrentQueue<Exception> backgroundExceptions = new ConcurrentQueue<Exception>();
		private ConcurrentQueue<IWatcher> parentWatchers = new ConcurrentQueue<IWatcher>();
		private long instanceIndex = 0;
		private TimeSpan connectionStart ;

		public ConnectionState(IZookeeperFactory zookeeperFactory, IEnsembleProvider ensembleProvider, TimeSpan sessionTimeout, TimeSpan connectionTimeout, IWatcher parentWatcher, ITracerDriver tracer, bool canBeReadOnly)
		{
			this.ensembleProvider = ensembleProvider;
			this.sessionTimeout = sessionTimeout;
			this.connectionTimeout = connectionTimeout;
			this.tracer = tracer;
			if ( parentWatcher != null )
			{
				parentWatchers.Enqueue (parentWatcher);

			}

			zooKeeper = new HandleHolder(zookeeperFactory, this, ensembleProvider, sessionTimeout, canBeReadOnly);
		}

		public IZooKeeper GetZooKeeper()
		{
			if ( SessionFailRetryLoop.SessionForThreadHasFailed() )
			{
				throw new SessionFailRetryLoop.SessionFailedException();
			}

			Exception exception;
			backgroundExceptions.TryDequeue (out exception);

			if ( exception != null )
			{
				tracer.AddCount("background-exceptions", 1);
				throw exception;
			}

			bool localIsConnected = isConnected;
			//Interlocked. (ref localIsConnected, isConnected);

			if ( !localIsConnected )
			{
				CheckTimeouts();
			}

			return zooKeeper.GetZooKeeper();
		}

		public bool IsConnected()
		{
			return isConnected;
		}

		public void Start()
		{
			log.Debug("Starting");
			ensembleProvider.Start();
			Reset();
		}


		public void Dispose() 
		{
			log.Debug("Closing");

			//CloseableUtils.closeQuietly(ensembleProvider);
			ensembleProvider.Dispose();
			try
			{
				zooKeeper.CloseAndClear();
			}
			catch ( Exception e )
			{
				throw new System.IO.IOException(e.Message,e);
			}
			finally
			{
				isConnected = false;
				//Interlocked.Exchange (ref isConnected, false);

			}
		}

		public void AddParentWatcher(IWatcher watcher)
		{
			parentWatchers.Enqueue (watcher);
		}

		public void RemoveParentWatcher(IWatcher watcher)
		{
			IWatcher tmpwatcher;
			parentWatchers.TryDequeue (out tmpwatcher);
		}

		public long GetInstanceIndex()
		{
			return instanceIndex;
		}


		public void Process(WatchedEvent @event)
		{
			if ( LOG_EVENTS )
			{
				log.Debug("ConnectState watcher: " + @event);
			}

			foreach ( IWatcher parentWatcher in parentWatchers )
			{
				TimeTrace timeTrace = new TimeTrace("connection-state-parent-process", tracer);
				parentWatcher.Process(@event);
				timeTrace.Commit();
			}

			bool wasConnected = isConnected ;
			//Interlocked.Exchange(ref wasConnected,isConnected);
			bool newIsConnected = wasConnected;
			if ( @event.Type == EventType.None )
			{
				newIsConnected = CheckState(@event.State, wasConnected);
			}

			if ( newIsConnected != wasConnected )
			{
				isConnected = newIsConnected;
				//Interlocked.Exchange(ref isConnected, newIsConnected);

				connectionStart = TimeSpan.FromTicks(System.DateTime.Now.Ticks);
			}
		}

		public IEnsembleProvider GetEnsembleProvider()
		{
			return ensembleProvider;
		}


		private  void CheckTimeouts() 
		{
			double minTimeout = Math.Min(sessionTimeout.TotalMilliseconds, connectionTimeout.TotalMilliseconds);
			double elapsed = TimeSpan.FromTicks(System.DateTime.Now.Ticks).TotalMilliseconds - connectionStart.TotalMilliseconds;
			if ( elapsed >= minTimeout )
			{
				if ( zooKeeper.HasNewConnectionString() )
				{
					HandleNewConnectionString();
				}
				else
				{
					double maxTimeout = Math.Max(sessionTimeout.TotalMilliseconds, connectionTimeout.TotalMilliseconds);
					if ( elapsed > maxTimeout )
					{
//						if ( !bool.getBoolean(DebugUtils.PROPERTY_DONT_LOG_CONNECTION_ISSUES) )
//						{
//							log.warn(String.format("Connection attempt unsuccessful after %d (greater than max timeout of %d). Resetting connection and trying again with a new connection.", elapsed, maxTimeout));
//						}
						Reset();
					}
					else
					{
						KeeperException.ConnectionLossException connectionLossException = new CuratorConnectionLossException();
//						if ( !bool.getBoolean(DebugUtils.PROPERTY_DONT_LOG_CONNECTION_ISSUES) )
//						{
//							log.error(String.format("Connection timed out for connection string (%s) and timeout (%d) / elapsed (%d)", zooKeeper.getConnectionString(), connectionTimeout, elapsed), connectionLossException);
//						}
						tracer.AddCount("connections-timed-out", 1);
						throw connectionLossException;
					}
				}
			}
		}


		private  void Reset() 
		{
			log.Debug("reset");

			Interlocked.Increment (ref instanceIndex);
			isConnected = false;
			//Interlocked.Exchange (ref isConnected, false);

			connectionStart = TimeSpan.FromTicks(System.DateTime.Now.Ticks);
			zooKeeper.CloseAndReset();
			zooKeeper.GetZooKeeper();   // initiate connection
		}

		private bool CheckState(KeeperState state, bool wasConnected)
		{
			bool isConnected = wasConnected;
			bool checkNewConnectionString = true;
			switch ( state )
			{
			default:
			case KeeperState.Disconnected:
				{
					isConnected = false;
					break;
				}

			case KeeperState.SyncConnected:

				{
					isConnected = true;
					break;
				}

//			case KeeperState.AuthFailed:
//				{
//					isConnected = false;
//					log.error("Authentication failed");
//					break;
//				}

			case KeeperState.Expired:
				{
					isConnected = false;
					checkNewConnectionString = false;
					HandleExpiredSession();
					break;
				}

//			case KeeperState.SaslAuthenticated:
//				{
//					// NOP
//					break;
//				}
			}

			if ( checkNewConnectionString && zooKeeper.HasNewConnectionString() )
			{
				HandleNewConnectionString();
			}

			return isConnected;
		}

		private void HandleNewConnectionString()
		{
			log.Info("Connection string changed");
			tracer.AddCount("connection-string-changed", 1);

			try
			{
				Reset();
			}
			catch ( Exception e )
			{
				QueueBackgroundException(e);
			}
		}

		private void HandleExpiredSession()
		{
			log.Warn("Session expired event received");
			tracer.AddCount("session-expired", 1);

			try
			{
				Reset();
			}
			catch ( Exception e )
			{
				QueueBackgroundException(e);
			}
		}


		private void QueueBackgroundException(Exception e)
		{
			while ( backgroundExceptions.Count >= MAX_BACKGROUND_EXCEPTIONS )
			{
				Exception result;
				backgroundExceptions.TryDequeue (out result);
			}
			backgroundExceptions.Enqueue (e);
		}
	}


}


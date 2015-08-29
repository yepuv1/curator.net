using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ZooKeeperNet;
using Curator;
using CuratorClient;

namespace Curator.NET.Test
{
    /**
 * <p>
 *     Utility to simulate a ZK session dying. See: <a href="http://wiki.apache.org/hadoop/ZooKeeper/FAQ#A4">ZooKeeper FAQ</a>
 * </p>
 *
 * <blockquote>
 *     In the case of testing we want to cause a problem, so to explicitly expire a session an
 *     application connects to ZooKeeper, saves the session id and password, creates another
 *     ZooKeeper handle with that id and password, and then closes the new handle. Since both
 *     handles reference the same session, the close on second handle will invalidate the session
 *     causing a SESSION_EXPIRED on the first handle.
 * </blockquote>
 */
    public class KillSession
    {
        /**
         * Kill the given ZK session
         *
         * @param client the client to kill
         * @param connectString server connection string
         * @throws Exception errors
         */
        public static void kill(IZooKeeper client, String connectString) 
        {
            kill(client, connectString, new Timing().forWaiting().milliseconds());
        }

        /**
         * Kill the given ZK session
         *
         * @param client the client to kill
         * @param connectString server connection string
         * @param maxMs max time ms to wait for kill
         * @throws Exception errors
         */
        public static void kill(IZooKeeper client, String connectString, int maxMs) 
        {
			System.Diagnostics.Debug.WriteLine ("Kill Start");
            long startTicks = (long)TimeSpan.FromTicks(System.DateTime.Now.Ticks).TotalMilliseconds;

			var sessionLostLatch = new AutoResetEvent(false);
			IWatcher sessionLostWatch = new CuratorWatcher((e)=> { if(e.State == KeeperState.Expired) sessionLostLatch.Set();});
            client.Exists("/___CURATOR_KILL_SESSION___" + System.DateTime.Now.Ticks, sessionLostWatch);

			var connectionLatch = new AutoResetEvent(false);
			var connectionWatcher = new CuratorWatcher((e)=> {
				if(e.State == KeeperState.SyncConnected){
					connectionLatch.Set();
				}
			});

             
            IZooKeeper zk = new ZooKeeper(connectString, TimeSpan.FromMilliseconds(maxMs), connectionWatcher, client.SessionId, client.SesionPassword);
            try
            {
                if ( !connectionLatch.WaitOne(maxMs) )
                {
                    throw new Exception("KillSession could not establish duplicate session");
                }
                try
                {
                        zk.Dispose();
                }
                finally
                {
                    zk = null;
                }

				while ( client.State.IsAlive() && !sessionLostLatch.WaitOne(100) )
                {
                    long elapsed = (long)TimeSpan.FromTicks(System.DateTime.Now.Ticks).TotalMilliseconds - startTicks;
                    if ( elapsed > maxMs )
                    {
                        throw new Exception("KillSession timed out waiting for session to expire");
                    }
                }
            }
            finally
            {
                if ( zk != null )
                {
                    zk.Dispose();
                }
            }
			System.Diagnostics.Debug.WriteLine ("Kill End");
    }
}
}

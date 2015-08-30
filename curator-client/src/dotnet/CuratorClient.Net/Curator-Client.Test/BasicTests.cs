using System;
using NUnit;
using NUnit.Framework;
using ZooKeeperNet;
using Curator;
using CuratorClient;
using System.Threading;

namespace Curator.NET.Test
{
	[TestFixture]
    public class BasicTests
    {

		protected const string connectionString = "127.0.0.1:2181";

        private IEnsembleProvider server = new FixedEnsembleProvider(connectionString);

		public BasicTests(){}

		[TestFixtureSetUp]
		public void FixtureSetup(){
		}

		[Test]
		public void SessionTermination(){

			var latch = new AutoResetEvent (false);
			var sxl = new AutoResetEvent (false);
			var conlatch = new AutoResetEvent (false);
			var w1 = new CuratorWatcher ((e) => {
				if(e.State == KeeperState.SyncConnected){
					latch.Set();
				}
				else if(e.State == KeeperState.Expired){
					sxl.Set();
				}
			});
			var zookeeper = new ZooKeeper(connectionString,TimeSpan.FromMilliseconds(10000),w1);
			latch.WaitOne (5000);

			using (var zk = new ZooKeeper (connectionString, TimeSpan.FromMilliseconds (2000), new CuratorWatcher ((e) => {
				if (e.State == KeeperState.SyncConnected)
					conlatch.Set ();
			}), zookeeper.SessionId, zookeeper.SesionPassword)) {

				if (!conlatch.WaitOne (5000)) {
					Assert.Fail ();
				} 
			};

			if (!sxl.WaitOne (20000)) {
				Assert.Fail ();
			} 

			try{
				var stat = zookeeper.Exists ("/test", false);
				if (stat == null) {
					System.Diagnostics.Debug.WriteLine ("Node does not exits");

				}
			}
			catch(KeeperException e){

				System.Diagnostics.Debug.WriteLine ("Session Expired");

			}

		}

        [Test]
        public void TestFactory()
        {
			ZooKeeperNet.IZooKeeper zookeeper = new ZooKeeper(connectionString,TimeSpan.FromMilliseconds(10000),new CuratorWatcher((e)=>{}));
            IZookeeperFactory zookeeperFactory = new DefaultZookeeperFactory();
			zookeeperFactory.NewZooKeeper(connectionString, TimeSpan.FromMilliseconds(10000), new CuratorWatcher((e)=>{}),false);
        }

        [Test]
        public  void TestExpiredSession()
        {
            var timing = new Timing();
			var latch = new AutoResetEvent(false);
			var watcher = new CuratorWatcher ((e) => {
				if (e.State == KeeperState.Expired)
					latch.Set ();
			});

			using (var client = new CuratorZookeeperClient(server.GetConnectionString(), timing.session(), timing.connection(), watcher, new RetryOneTime(TimeSpan.FromMilliseconds(2))))
            {
                client.Start();

                bool firstTime = true;
                RetryLoop.CallWithRetry<bool>(client,() =>
                {

                    if (firstTime)
                    {
                        try
                        {
							var stat = client.GetZooKeeper().Exists("/foo", false);
								if(stat == null){
								
									client.GetZooKeeper().Create("/foo", new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
								}

                            
                        }
                        catch (KeeperException.NodeExistsException ignore)
                        {

                        }

                        KillSession.kill(client.GetZooKeeper(), server.GetConnectionString());
						Assert.IsFalse(timing.awaitLatch(latch));
                        
                    }

                    IZooKeeper zooKeeper = client.GetZooKeeper();
                    client.BlockUntilConnectedOrTimedOut();
                    Assert.IsNotNull(zooKeeper.Exists("/foo", false));
                   
                    return true;
                });

            }
                


        }

        [Test]
        public void testReconnect()
        {
            using (CuratorZookeeperClient client = new CuratorZookeeperClient(server.GetConnectionString(), 10000, 10000, null, new RetryOneTime(TimeSpan.FromSeconds(1))))
            {
                client.Start();

                client.BlockUntilConnectedOrTimedOut();

                byte[] writtenData = { 1, 2, 3 };

				var stat = client.GetZooKeeper ().Exists ("/test", false);
				if (stat == null) {
					client.GetZooKeeper ().Create ("/test", writtenData, Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
				}
                               
                Assert.IsTrue(client.BlockUntilConnectedOrTimedOut());
                byte[] readData = client.GetZooKeeper().GetData("/test", false, null);
                Assert.AreEqual(readData, writtenData);
            }
        }

        [Test]
        public void testSimple() 
        {
            using (CuratorZookeeperClient client = new CuratorZookeeperClient(server.GetConnectionString(), 10000, 10000, null, new RetryOneTime(TimeSpan.FromSeconds(1))))
            {
                client.Start();
               
                client.BlockUntilConnectedOrTimedOut();
				var stat = client.GetZooKeeper ().Exists ("/test",false);

				if (stat != null) {
					client.GetZooKeeper ().Delete ("/test", -1);
				}
                String path = client.GetZooKeeper().Create("/test", new byte[] { 1, 2, 3 }, Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
                Assert.AreEqual(path, "/test");
               
            }
        }

        [Test]
        public void testBackgroundConnect() 
        {
             int CONNECTION_TIMEOUT_MS = 4000;

            using (CuratorZookeeperClient client = new CuratorZookeeperClient(server.GetConnectionString(), 10000, CONNECTION_TIMEOUT_MS, null, new RetryOneTime(TimeSpan.FromSeconds(1))))
            {
               
                Assert.False(client.IsConnected());
                client.Start();

                outer:
                do
                {
                    for (int i = 0; i < (CONNECTION_TIMEOUT_MS / 1000); ++i)
                    {
                        if (client.IsConnected())
                        {
                            goto outer;
                        }

                        Thread.Sleep(CONNECTION_TIMEOUT_MS);
                    }

                    Assert.Fail();
                } while (false);
               
            }
    }
			
    }
}

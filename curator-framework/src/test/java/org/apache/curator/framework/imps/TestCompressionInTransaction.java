/**
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
package org.apache.curator.framework.imps;

import org.apache.curator.test.BaseClassForTests;
import org.apache.curator.utils.CloseableUtils;
import org.apache.curator.framework.CuratorFramework;
import org.apache.curator.framework.CuratorFrameworkFactory;
import org.apache.curator.framework.api.CompressionProvider;
import org.apache.curator.retry.RetryOneTime;
import org.testng.Assert;
import org.testng.annotations.Test;
import java.util.concurrent.atomic.AtomicInteger;

public class TestCompressionInTransaction extends BaseClassForTests
{
    @Test
    public void testSimple() throws Exception
    {
        final String path1 = "/a";
        final String path2 = "/a/b";
        
        final byte[]            data1 = "here's a string".getBytes();
        final byte[]            data2 = "here's another string".getBytes();

        CuratorFramework        client = CuratorFrameworkFactory.newClient(server.getConnectString(), new RetryOneTime(1));
        try
        {
            client.start();

            client.inTransaction().create().compressed().forPath(path1, data1).and().
            create().compressed().forPath(path2, data2).and().commit();

            Assert.assertNotEquals(data1, client.getData().forPath(path1));
            Assert.assertEquals(data1.length, client.getData().decompressed().forPath(path1).length);
            
            Assert.assertNotEquals(data2, client.getData().forPath(path2));
            Assert.assertEquals(data2.length, client.getData().decompressed().forPath(path2).length);            
        }
        finally
        {
            CloseableUtils.closeQuietly(client);
        }
    }
}

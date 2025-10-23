#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion

using System;
using System.Threading;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

[TestClass]
public class TestSimpleReadWriteLocking : BaseThreadedReaderWriterTest<LockFactory<SimpleReadWriteLocking>>
{
    [TestMethod]
    public void ExplicitSyncObject()
    {
        var obj = new object();
        var l = new SimpleReadWriteLocking(obj);
        using(new ThreadedWriter(l))
            Assert.IsFalse(Monitor.TryEnter(obj, 0));
        l.Dispose();
    }
    [TestMethod]
    public void ReadToWriteFails()
    {
        using (ILockStrategy l = LockFactory.Create())
        using (l.Read())
            Assert.IsFalse(l.TryWrite(10));
    }
    [TestMethod]
    public void DisposedWithReaders()
    {
		var l = LockFactory.Create();
		var thread = new ThreadedReader(l);

		Assert.Throws<InvalidOperationException>(() =>
        {
            try
            {
                l.Dispose();
            }
            finally
            {
                try { thread.Dispose(); }
                catch (ObjectDisposedException)
                { }
            }
        });
    }

    [TestMethod]
    public void DisposedWithWriters()
    {
		var l = LockFactory.Create();
		var thread = new ThreadedWriter(l);

		Assert.Throws<InvalidOperationException>(() =>
        {
            try
            {
                l.Dispose();
            }
            finally
            {
                try { thread.Dispose(); }
                catch (ObjectDisposedException)
                { }
            }
        });
    }
}
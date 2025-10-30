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
public class TestLockingStructs
{
    protected readonly ILockFactory LockFactory = new LockFactory<SimpleReadWriteLocking>();

    [TestMethod]
    public void TestReadLockSuccess()
    {
		using ILockStrategy l = LockFactory.Create();
		using ReadLock r = new ReadLock(l, 0);
		Assert.IsTrue(r.HasReadLock);
	}

    [TestMethod]
    public void TestReadLockTimeout()
    {
        using (ILockStrategy l = LockFactory.Create())
        using (new ThreadedWriter(l))
        using (ReadLock r = new ReadLock(l, 0))
            Assert.IsFalse(r.HasReadLock);
    }

    [TestMethod]
    public void TestYouCanDisposeReadLock()
    {
		using ILockStrategy l = LockFactory.Create();
#pragma warning disable CA1859 // simulate double dispose
		using IDisposable r = ReadLock.Acquire(l, 0);
#pragma warning restore CA1859 
		r.Dispose();//since the using statement has the same boxed pointer to r, we are allowed to dispose
	}

    [TestMethod]
    public void TestIdiotReaderUsesDispose()
    {
		using ILockStrategy l = LockFactory.Create();
		var r = new ReadLock(l, 0);
		Assert.IsTrue(r.HasReadLock);

		((IDisposable)r).Dispose(); 
		Assert.Throws<SynchronizationLockException>(() => ((IDisposable)r).Dispose()); //You cannot do this, the ReadLock is disposed.
    }

    [TestMethod]
    public void TestWriteLockSuccess()
    {
		using ILockStrategy l = LockFactory.Create();
		using WriteLock w = new WriteLock(l, 0);
		Assert.IsTrue(w.HasWriteLock);
	}

    [TestMethod]
    public void TestWriteLockTimeout()
    {
        using (ILockStrategy l = LockFactory.Create())
        using (new ThreadedWriter(l))
        using (WriteLock w = new WriteLock(l, 0))
            Assert.IsFalse(w.HasWriteLock);
    }

    [TestMethod]
    public void TestYouCanDisposeWriteLock()
    {
		using ILockStrategy l = LockFactory.Create();
#pragma warning disable CA1859 // Simulate using
		using IDisposable w = l.Write();
#pragma warning restore CA1859 
		w.Dispose();//since the using statement has the same boxed pointer to w, we are allowed to dispose
	}

    [TestMethod]
    public void TestIdiotWriterUsesDispose()
    {
		using ILockStrategy l = LockFactory.Create();
		var w = new WriteLock(l, 0);
		Assert.IsTrue(w.HasWriteLock);

		((IDisposable)w).Dispose();
		Assert.Throws<SynchronizationLockException>(() => ((IDisposable)w).Dispose()); //You cannot do this, the write lock is disposed.
    }

    [TestMethod]
    public void TestSafeLockSuccess()
    {
        object instance = new object();
        using (new SafeLock(instance))
        { }
    }
    [TestMethod]
    public void TestSafeLockSuccessWithTException()
    {
        object instance = new object();
        using (new SafeLock<InvalidOperationException>(instance))
        { }
    }

    [TestMethod]
    public void TestSafeLockTimeout()
    {
		var instance = new object();
        using var wx = new ThreadedWriter(new SimpleReadWriteLocking(instance));
	    Assert.Throws<TimeoutException>(() => new SafeLock(instance, 0));
    }

    [TestMethod]
    public void TestSafeLockTimeoutWithTException()
    {
		var instance = new object();
        using var xw = new ThreadedWriter(new SimpleReadWriteLocking(instance));

		Assert.Throws<ArgumentOutOfRangeException>(() => new SafeLock<ArgumentOutOfRangeException>(instance, 0));
    }

    [TestMethod]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Testing dispose")]
	public void TestYouCanDisposeSafeLock()
    {
        object instance = new object();
		using IDisposable safeLock = new SafeLock(instance, 0);
		safeLock.Dispose();//since the using statement has the same boxed pointer to r, we are allowed to dispose
	}

    [TestMethod]
    public void TestIdiotUsesSafeLockDispose()
    {
        object instance = new object();
		var safeLock = new SafeLock(instance, 0);

		((IDisposable)safeLock).Dispose();
		Assert.Throws<SynchronizationLockException>(() => ((IDisposable)safeLock).Dispose());
	}
}

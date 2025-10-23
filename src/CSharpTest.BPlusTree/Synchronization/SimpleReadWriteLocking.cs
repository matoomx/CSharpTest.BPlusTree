#region Copyright 2010-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Collections.Generic;

/// <summary>
/// provides a simple and fast, reader-writer lock, does not support read->write upgrades,
/// if you need an upgradeable lock, use UpgradeableReadWriteLocking
/// </summary>
public sealed class SimpleReadWriteLocking : ILockStrategy
{
    /// <summary> Max number of Spin loops before polling the _event </summary>
    static readonly int SpinLoops = SpinLoopsDefault();
    /// <summary> Number of iterations used for Thread.SpinWait(x) </summary>
    static readonly int SpinWaitTime = SpinWaitTimeDefault();
    /// <summary> Setup of the SpinWaitTime/SpinLoops by processor count </summary>
    static int SpinWaitTimeDefault()
    { 
        try 
        { 
            if (Environment.ProcessorCount > 0) 
                return 25; 
        } 
        catch 
        { 
        }
		return 0;
	}

	static int SpinLoopsDefault()
	{
		try
		{
			if (Environment.ProcessorCount > 0)
				return 100;
		}
		catch
		{
		}
		return 0;
	}

	/// <summary> The event used to wake a waiting writer when a read lock is released </summary>
	AutoResetEvent _event;
    /// <summary> The syncronization object writers and potential readers use to lock </summary>
    object _sync;
    /// <summary> The total number of read locks on this lock </summary>
    int _readersCount;
    /// <summary> The number of readers the pending writer is waiting upon </summary>
    int _targetReaders;
    /// <summary> The managed thread id for the thread holding the write lock </summary>
    int _exclusiveThreadId;
    /// <summary> The number of times the write lock thread has acquired a write lock </summary>
    int _writeRecursiveCount;

    /// <summary>
    /// Constructs the reader-writer lock using 'this' as the syncronization object
    /// </summary>
    public SimpleReadWriteLocking()
    {
        _targetReaders = -1;
        _sync = this;
    }

    /// <summary>
    /// Constructs the reader-writer lock using the specified object for syncronization
    /// </summary>
    public SimpleReadWriteLocking(object syncRoot)
    {
        _sync = syncRoot;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        object exit = Interlocked.Exchange(ref _sync, null);
        _event?.Close();

        if (exit == null)
            return;

        using(new SafeLock<InvalidOperationException>(exit, 0))
        {
            if (_readersCount > 0)
                throw new InvalidOperationException();
        }
    }
    
    /// <summary>
    /// Returns true if the lock was successfully obtained within the timeout specified
    /// </summary>
    public bool TryRead(int millisecondsTimeout)
    {
		ObjectDisposedException.ThrowIf(_sync == null, this);
		bool success = false;
        try { } 
        finally 
        {
            // First lock the 'writer lock' to ensure there are no writers
            if (Monitor.TryEnter(_sync, millisecondsTimeout))
            {
                // Safe to increment the read counter since there are no writers
                Interlocked.Increment(ref _readersCount);

                // Release the lock, we are done
                Monitor.Exit(_sync);
                success = true;
            }
        }
        return success;
    }

    /// <summary>
    /// Releases a read lock
    /// </summary>
    public void ReleaseRead()
    {
        ObjectDisposedException.ThrowIf(_sync == null, this);

        // Decrement the reader count and, if we are the last, set the wake event for a writer
        int newCount = Interlocked.Decrement(ref _readersCount);
        if (_targetReaders == newCount && _event != null)
            _event.Set();

        if (newCount < 0)
        {
            Interlocked.Increment(ref newCount);
            throw new SynchronizationLockException();
        }
    }

    /// <summary>
    /// Returns true if the lock was successfully obtained within the timeout specified
    /// </summary>
    public bool TryWrite(int millisecondsTimeout)
    {
        bool success = false;
        try { } 
        finally 
        {
            ObjectDisposedException.ThrowIf(_sync == null, this);
            // First obtain the 'writer lock':
            if (Monitor.TryEnter(_sync, millisecondsTimeout))
            {
                // Now that we have the lock, wait for the readers to release
                if (WaitForExclusive(0, millisecondsTimeout))
                    success = true;
                else
                    Monitor.Exit(_sync);
            }
        }
        return success;
    }

    /// <summary>
    /// This is the only real work to be done, once we've acquired the write lock
    /// we have to wait for all readers to complete.  If/when that happens we can
    /// then own the write lock.  The case where this does not take place is when
    /// a thread that already owns the lock calls us to lock again.  In this case
    /// we can just return success and ignore the outstanding read requests.  The
    /// major problem with this approach is that if function A() does a read-lock
    /// and calls function B() which does a write lock, this will fail.  So the
    /// solution is to either use the upgradeable version (see the derived class 
    /// UpgradableReadWriteLocking) and upgrade, or to start with a write lock in
    /// function A().
    /// </summary>
    private bool WaitForExclusive(int targetReaders, int millisecondsTimeout)
    {
        // if this thread is already the writer, we can just return
        int threadId = Environment.CurrentManagedThreadId;
        if (_exclusiveThreadId == threadId)
        {
            _writeRecursiveCount++;
            return true;
        }
        // convert the timeout to a positive number
        if (millisecondsTimeout < 0)
            millisecondsTimeout = int.MaxValue;

        // set the number of readers we are looking for so that when that number
        // is reached by ReleaseRead it will signal us.  Obviously the event and
        // several things about it are not syncronized and as such require the
        // polling loop we are about to start...
        if (_event == null && _readersCount > targetReaders)
            _event = new AutoResetEvent(false);
        _targetReaders = targetReaders;

        // If SpinWaitTime is zero we are on a single-proc host and don't want to
        // consume the CPU, otherwise we want to start polling by using a tight
        // spin loop waiting for the readers to release.  You might notice that 
        // our timeout is anything but accurate here, but it should prove to be 
        // 'ballpark' close :)
        int loop = 0;
        int spinLoops = Math.Min(millisecondsTimeout, SpinLoops);
        while (_readersCount > targetReaders && loop < millisecondsTimeout)
        {
            // first few rounds we use a spin loop
            if (loop < spinLoops)
            {
                loop++;
                Thread.SpinWait(SpinWaitTime);
                continue;
            }
            // if we are still waiting on readers after spinning for a few loops
            // we can just start polling the event.  
            if (!_event.WaitOne(10, false))
                loop += 10;
        }

        // clear the expected reader count, so sense in reader threads setting the
        // event over and over again.
        _targetReaders = -1;

        // if we failed to hit our target reader count we just leave.
        if (_readersCount > targetReaders)
            return false;

        // set the number of writers, increment the version and we are done
        _writeRecursiveCount = 1;
        _exclusiveThreadId = threadId;
        return true;
    }

    /// <summary>
    /// Releases a writer lock
    /// </summary>
    public void ReleaseWrite()
    {
        ObjectDisposedException.ThrowIf(_sync == null, this);

        // if we are the exclusive thread, and the last ReleaseWrite that will be called, we can then
        // clear the captured thread id for the exclusive writer.
        if (_exclusiveThreadId == Environment.CurrentManagedThreadId && --_writeRecursiveCount == 0)
            _exclusiveThreadId = 0;
        // Now just release the lock once (btw, this thread may still have locks on this)
        Monitor.Exit(_sync);
    }

    /// <summary>
    /// Returns a reader lock that can be elevated to a write lock
    /// </summary>
    public ReadLock Read() 
    { 
        return ReadLock.Acquire(this, -1); 
    }

    /// <summary>
    /// Returns a reader lock that can be elevated to a write lock
    /// </summary>
    /// <exception cref="System.TimeoutException"/>
    public ReadLock Read(int millisecondsTimeout) 
    { 
        return ReadLock.Acquire(this, millisecondsTimeout); 
    }

    /// <summary>
    /// Returns a read and write lock
    /// </summary>
    public WriteLock Write() 
    { 
        return WriteLock.Acquire(this, -1); 
    }

    /// <summary>
    /// Returns a read and write lock
    /// </summary>
    /// <exception cref="System.TimeoutException"/>
    public WriteLock Write(int millisecondsTimeout) 
    { 
        return WriteLock.Acquire(this, millisecondsTimeout); 
    }
}

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
using System.Threading.Tasks;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

public class ThreadedReader : ThreadedWriter
{
    public ThreadedReader(ILockStrategy lck): base(lck)
    {
    }
    protected override IDisposable Lock()
    {
        return _lck.Read(100);
    }
}

public class ThreadedWriter : IDisposable
{
    private readonly ManualResetEvent _started;
    private readonly ManualResetEvent _complete;
    private readonly Task _async;
    private readonly Action _delegate;
    protected readonly ILockStrategy _lck;
    private bool _locked;

    public ThreadedWriter(ILockStrategy lck)
    {
        _started = new ManualResetEvent(false);
        _complete = new ManualResetEvent(false);

        _lck = lck;
        _delegate = HoldLock;
        _async = Task.Run(() => _delegate());
        if (!_started.WaitOne(1000, false))
        {
            _async.GetAwaiter().GetResult();
            Assert.Fail("Unable to acquire lock");
        }
        Assert.IsTrue(_locked);
    }

    public void Dispose()
    {
        if (_locked)
        {
            _locked = false;
            _complete.Set();
            _async.GetAwaiter().GetResult();
        }

        _started.Dispose();
        _complete.Dispose();
        _async?.Dispose();

        GC.SuppressFinalize(this);
    }

    void HoldLock()
    {
        using (Lock())
        {
            _locked = true;
            _started.Set();
            _complete.WaitOne();
        }
    }

    protected virtual IDisposable Lock() { return _lck.Write(100); }
}

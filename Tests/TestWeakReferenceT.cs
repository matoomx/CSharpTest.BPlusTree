﻿#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

[TestClass]
public class TestWeakReferenceT
{
    static bool _destroyed;
    class MyObject
    {
        ~MyObject()
        {
            _destroyed = true;
        }
    }

    [TestMethod]
    public void TestDestoryed()
    {
        CSharpTest.Collections.Generic.WeakReference<MyObject> r = CreateWeakRef();

        GC.Collect(2, GCCollectionMode.Forced);
        GC.WaitForPendingFinalizers();

        Assert.IsTrue(_destroyed);
		Assert.IsFalse(r.IsAlive);
		Assert.IsNull(r.Target);
        Assert.IsFalse(r.TryGetTarget(out MyObject tmp));
    }

	[MethodImpl(MethodImplOptions.NoInlining)]
	private CSharpTest.Collections.Generic.WeakReference<MyObject> CreateWeakRef()
    {
		MyObject obj = new MyObject();

		var r = new CSharpTest.Collections.Generic.WeakReference<MyObject>(obj);
		Assert.IsTrue(r.IsAlive);
		Assert.IsNotNull(r.Target);
		Assert.IsTrue(r.TryGetTarget(out MyObject test));
		Assert.IsTrue(ReferenceEquals(obj, test));
		_destroyed = false;

		GC.KeepAlive(obj);

		return r;
	}


	[TestMethod]
    public void TestReplaceTarget()
    {
        string value1 = "Testing Value - 1";
        string value2 = "Testing Value - 2";
        var r = new CSharpTest.Collections.Generic.WeakReference<string>(value1);

		Assert.IsTrue(r.TryGetTarget(out string tmp) && tmp == value1);

		r.Target = value2;
        Assert.IsTrue(r.TryGetTarget(out tmp) && tmp == value2);
    }

    [TestMethod]
    public void TestReplaceBadTypeTarget()
    {
        string value1 = "Testing Value - 1";
        object value2 = new MyObject();
        var r = new CSharpTest.Collections.Generic.WeakReference<string>(value1);

		Assert.IsTrue(r.TryGetTarget(out string tmp) && tmp == value1);

		((WeakReference)r).Target = value2; //incorrect type...
        Assert.IsFalse(r.IsAlive);
        Assert.IsNull(r.Target);
        Assert.IsFalse(r.TryGetTarget(out tmp));

        Assert.IsTrue(ReferenceEquals(value2, ((WeakReference)r).Target));
    }
}

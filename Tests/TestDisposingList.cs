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
using System.Collections.Generic;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

[TestClass]
public partial class TestDisposingList
{
    static readonly List<IDisposable> disposeOrder = new List<IDisposable>();
    class DisposeInOrder : IDisposable
    {
        public void Dispose()
        {
            disposeOrder.Add(this);
        }
    }

    [TestMethod]
    public void TestNonGeneric()
    {
        disposeOrder.Clear();
        var list = new DisposingList();
		var a = new DisposeInOrder();
		var b = new DisposeInOrder();

        list.Add(a);
        list.Add(b);
        list.Add(null);
        list.Dispose();

        //Removed from list?
        Assert.IsEmpty(list);
        //All were disposed?
        Assert.HasCount(2, disposeOrder);
        //Disposed in reverse order of creation?
        Assert.IsTrue(object.ReferenceEquals(b, disposeOrder[0]));
        Assert.IsTrue(object.ReferenceEquals(a, disposeOrder[1]));

        Assert.HasCount(2, new DisposingList([a, b]));
        Assert.IsEmpty(new DisposingList(5));
    }

    [TestMethod]
    public void TestGeneric()
    {
        disposeOrder.Clear();

		var list = new DisposingList<DisposeInOrder>();
		var a = new DisposeInOrder();
		var b = new DisposeInOrder();

        list.Add(a);
        list.Add(b);
        list.Add(null);
        list.Dispose();

        //Removed from list?
        Assert.IsEmpty(list);
        //All were disposed?
        Assert.HasCount(2, disposeOrder);
        //Disposed in reverse order of creation?
        Assert.IsTrue(object.ReferenceEquals(b, disposeOrder[0]));
        Assert.IsTrue(object.ReferenceEquals(a, disposeOrder[1]));

        Assert.HasCount(2, new DisposingList<DisposeInOrder>([a, b]));
        Assert.IsEmpty(new DisposingList<DisposeInOrder>(5));
    }
}

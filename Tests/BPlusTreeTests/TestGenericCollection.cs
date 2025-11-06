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

using System.Collections.Generic;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

public abstract class TestGenericCollection<TList, TItem> where TList : ICollection<TItem>
{
	protected abstract TList CollectionFactory();

	protected abstract TItem[] GetSample();

    protected TList CreateSample(TItem[] items)
    {
        TList list = CollectionFactory();

        int count = 0;
        Assert.HasCount(count, list);

        foreach (TItem item in items)
        {
            list.Add(item);
            Assert.HasCount(++count, list);
        }
        return list;
    }

    [TestMethod]
    public void TestAddRemove()
    {
        TList list = CollectionFactory();
        TItem[] items = GetSample();

        int count = 0;
        Assert.HasCount(count, list);

        foreach (TItem item in items)
        {
            list.Add(item);
            Assert.HasCount(++count, list);
        }
        foreach (TItem item in items)
        {
            Assert.IsTrue(list.Remove(item));
            Assert.HasCount(--count, list);
        }
    }

    [TestMethod]
    public void TestAddReverseRemove()
    {
        TList list = CollectionFactory();
        TItem[] items = GetSample();

        int count = 0;
        Assert.HasCount(count, list);

        foreach (TItem item in items)
        {
            list.Add(item);
            Assert.HasCount(++count, list);
        }
        for (int ix = items.Length - 1; ix >= 0; ix--)
        {
            Assert.IsTrue(list.Remove(items[ix]));
            Assert.HasCount(--count, list);
        }
    }

    [TestMethod]
    public void TestClear()
    {
        TList list = CollectionFactory();
        TItem[] items = GetSample();

        foreach (TItem item in items)
            list.Add(item);

        Assert.HasCount(items.Length, list);

        Assert.AreNotEqual(0, list.Count);
        list.Clear();
        Assert.IsEmpty(list);
    }

    [TestMethod]
    public void TestContains()
    {
        TList list = CollectionFactory();
        TItem[] items = GetSample();

        foreach (TItem item in items)
            list.Add(item);
        
        Assert.HasCount(items.Length, list);

        foreach (TItem item in items)
            Assert.Contains(item, list);
    }

    [TestMethod]
    public void TestCopyTo()
    {
        TList list = CollectionFactory();
        List<TItem> items = new List<TItem>(GetSample());

        foreach (TItem item in items)
            list.Add(item);
        
        Assert.HasCount(items.Count, list);

        TItem[] copy = new TItem[items.Count + 1];
        list.CopyTo(copy, 1);
        Assert.AreEqual(default(TItem), copy[0]);

        for (int i = 1; i < copy.Length; i++)
            Assert.IsTrue(items.Remove(copy[i]));

        Assert.IsEmpty(items);
    }

    [TestMethod]
    public void TestIsReadOnly()
    {
        Assert.IsFalse(CollectionFactory().IsReadOnly);
    }

    [TestMethod]
    public void TestGetEnumerator()
    {
        TList list = CollectionFactory();
        List<TItem> items = new List<TItem>(GetSample());

        foreach (TItem item in items)
            list.Add(item);

        Assert.HasCount(items.Count, list);

        foreach (TItem item in list)
            Assert.IsTrue(items.Remove(item));

        Assert.IsEmpty(items);
    }

    [TestMethod]
    public void TestGetEnumerator2()
    {
        TList list = CollectionFactory();
        List<TItem> items = new List<TItem>(GetSample());

        foreach (TItem item in items)
            list.Add(item);

        Assert.HasCount(items.Count, list);

        foreach (TItem item in ((System.Collections.IEnumerable)list))
            Assert.IsTrue(items.Remove(item));

        Assert.IsEmpty(items);
    }

    public static void VerifyCollection<T, TC>(IEqualityComparer<T> comparer, ICollection<T> expected, TC collection) where TC : ICollection<T>
    {
        Assert.AreEqual(expected.IsReadOnly, collection.IsReadOnly);
        Assert.HasCount(expected.Count, collection);
        CompareEnumerations(comparer, expected, collection);
        using (var a = expected.GetEnumerator())
        using (var b = collection.GetEnumerator())
        {
            bool result;
            Assert.IsTrue(b.MoveNext());
            b.Reset();
            Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
            while (result)
            {
                Assert.IsTrue(comparer.Equals(a.Current, b.Current));
                Assert.IsTrue(comparer.Equals(a.Current, (T)((System.Collections.IEnumerator)b).Current));
                Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
            }
        }

        T[] items = new T[10 + collection.Count];
        collection.CopyTo(items, 5);
        Array.Copy(items, 5, items, 0, collection.Count);
        Array.Resize(ref items, collection.Count);
        CompareEnumerations(comparer, expected, collection);

        for( int i=0; i < 5; i++)
            Assert.Contains(items[i], collection);
    }

    public static void CompareEnumerations<T>(IEqualityComparer<T> comparer, IEnumerable<T> expected, IEnumerable<T> collection)
    {
		using var a = expected.GetEnumerator();
		using var b = collection.GetEnumerator();
		bool result;
		Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
		while (result)
		{
			Assert.IsTrue(comparer.Equals(a.Current, b.Current));
			Assert.AreEqual(result = a.MoveNext(), b.MoveNext());
		}
	}
}
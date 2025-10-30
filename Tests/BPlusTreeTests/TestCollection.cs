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
using System.Collections.Generic;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

public abstract class TestCollection<TList, TFactory, TItem> where TList : ICollection<TItem>, IDisposable where TFactory : IFactory<TList>, new()
{
    protected abstract TItem[] GetSample();
    protected readonly TFactory Factory = new TFactory();

    [TestMethod]
    public void TestAddRemove()
    {
		using var list = Factory.Create();
		var items = GetSample();

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
		using var list = Factory.Create();
		var items = GetSample();

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
		using var list = Factory.Create();
		var items = GetSample();

		foreach (TItem item in items)
			list.Add(item);
		Assert.HasCount(items.Length, list);

		Assert.AreNotEqual(0, list.Count);
		list.Clear();
		Assert.HasCount(0, list);
	}

    [TestMethod]
    public void TestContains()
    {
		using var list = Factory.Create();
		var items = GetSample();

		foreach (TItem item in items)
			list.Add(item);

		Assert.HasCount(items.Length, list);

		foreach (TItem item in items)
			Assert.Contains(item, list);
	}

    [TestMethod]
    public void TestCopyTo()
    {
		using var list = Factory.Create();
		var items = new List<TItem>(GetSample());

		foreach (TItem item in items)
			list.Add(item);

		Assert.HasCount(items.Count, list);

		TItem[] copy = new TItem[items.Count + 1];
		list.CopyTo(copy, 1);
		Assert.AreEqual(default, copy[0]);

		for (int i = 1; i < copy.Length; i++)
			Assert.IsTrue(items.Remove(copy[i]));

		Assert.IsEmpty(items);
	}

    [TestMethod]
    public void TestIsReadOnly()
    {
		using var list = Factory.Create();
		Assert.IsFalse(list.IsReadOnly);
	}

    [TestMethod]
    public void TestGetEnumerator()
    {
		using var list = Factory.Create();
		var items = new List<TItem>(GetSample());

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
		using var list = Factory.Create();
		var items = new List<TItem>(GetSample());

		foreach (TItem item in items)
			list.Add(item);

		Assert.HasCount(items.Count, list);

		foreach (TItem item in (System.Collections.IEnumerable)list)
			Assert.IsTrue(items.Remove(item));

		Assert.IsEmpty(items);
	}
}
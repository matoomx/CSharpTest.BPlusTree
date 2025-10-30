#region Copyright 2012-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

[TestClass]
public class TestOrderedEnumeration
{
    [TestMethod]
    public void TestKeyValueComparer()
    {
        var cmp = new KeyValueComparer<int, int>();
        Assert.IsTrue(ReferenceEquals(Comparer<int>.Default, cmp.Comparer));
        Assert.IsTrue(ReferenceEquals(Comparer<int>.Default, KeyValueComparer<int, int>.Default.Comparer));

        Assert.AreEqual(-1, cmp.Compare(new KeyValuePair<int, int>(1, 1), new KeyValuePair<int, int>(2, 1)));
        Assert.AreEqual(0, cmp.Compare(new KeyValuePair<int, int>(1, 1), new KeyValuePair<int, int>(1, 2)));
        Assert.AreEqual(1, cmp.Compare(new KeyValuePair<int, int>(2, 1), new KeyValuePair<int, int>(1, 1)));
    }

    [TestMethod]
    public void TestMergeSortBasicOverloads()
    {
        Guid[] test, arrTest = new Guid[255];
        for (int i = 1; i < arrTest.Length; i++) 
            arrTest[i] = Guid.NewGuid();
        Guid[] expect = (Guid[])arrTest.Clone();
        Array.Sort(expect);

        test = (Guid[])arrTest.Clone();
        MergeSort.Sort(test);
        AssertArrayEquals(Comparer<Guid>.Default, expect, test);

        test = (Guid[])arrTest.Clone();
        MergeSort.Sort(test, delegate(Guid x, Guid y) { return x.CompareTo(y); });
        AssertArrayEquals(Comparer<Guid>.Default, expect, test);

        test = (Guid[])arrTest.Clone();
        MergeSort.Sort(test, Comparer<Guid>.Default);
        AssertArrayEquals(Comparer<Guid>.Default, expect, test);
    }

    static void AssertArrayEquals<T>(IComparer<T> cmp, T[] x, T[] y)
    {
        Assert.HasCount(x.Length, y);

        for (int i = 0; i < x.Length; i++)
            Assert.AreEqual(0, cmp.Compare(x[i], y[i]));
    }

    [TestMethod]
    public void TestMergeSortRangeOverloads()
    {
        char[] input = "zrogera".ToCharArray();
        MergeSort.Sort(input, 1, 5, Comparer<char>.Default);
        Assert.AreEqual("zegorra", new string(input));

        input = "zrogera".ToCharArray();
        MergeSort.Sort(input, 1, 5, delegate(char x, char y) { return y.CompareTo(x); } );
        Assert.AreEqual("zrrogea", new string(input));
    }

    class AllEqual : IComparable<AllEqual>
    {
        public int CompareTo(AllEqual other)
        { 
            return 0; 
        }
    }

    [TestMethod]
    public void TestMergeSortStable()
    {
        AllEqual[] set = [new AllEqual(), new AllEqual(), new AllEqual()];
        AllEqual[] copy = (AllEqual[])set.Clone();
        MergeSort.Sort(copy);
        Assert.IsTrue(ReferenceEquals(set[0], copy[0]));
        Assert.IsTrue(ReferenceEquals(set[1], copy[1]));
        Assert.IsTrue(ReferenceEquals(set[2], copy[2]));
    }

    [TestMethod]
    public void TestOrderedEnum()
    {
        byte[] input = new byte[256];
        new Random().NextBytes(input);

        byte last = 0;
        foreach (byte b in new OrderedEnumeration<byte>(input))
        {
            Assert.IsLessThanOrEqualTo(b, last);
            last = b;
        }
    }

    class ReverseOrder<T> : IComparer<T>
    {
        readonly IComparer<T> _compare;
        public ReverseOrder(IComparer<T> compare)
        { 
            _compare = compare; 
        }
        public int Compare(T x, T y)
        {
            return -_compare.Compare(x, y);
        }
    }

    [TestMethod]
    public void TestDedupFirst()
    {
		AllEqual[] set = [new AllEqual(), new AllEqual(), new AllEqual()];
        var list = new List<AllEqual>(OrderedEnumeration<AllEqual>.WithDuplicateHandling(set, Comparer<AllEqual>.Default, DuplicateHandling.FirstValueWins));
        Assert.HasCount(1, list);
        Assert.IsTrue(ReferenceEquals(set[0], list[0]));

        list = new List<AllEqual>(OrderedEnumeration<AllEqual>.WithDuplicateHandling(set, Comparer<AllEqual>.Default, DuplicateHandling.LastValueWins));
        Assert.HasCount(1, list);
        Assert.IsTrue(ReferenceEquals(set[2], list[0]));

        list = new List<AllEqual>(OrderedEnumeration<AllEqual>.WithDuplicateHandling(set, Comparer<AllEqual>.Default, DuplicateHandling.None));
        Assert.HasCount(3, list);
        Assert.IsTrue(ReferenceEquals(set[0], list[0]));
        Assert.IsTrue(ReferenceEquals(set[1], list[1]));
        Assert.IsTrue(ReferenceEquals(set[2], list[2]));
        Assert.Throws<ArgumentException>(() => new List<AllEqual>(OrderedEnumeration<AllEqual>.WithDuplicateHandling(set, Comparer<AllEqual>.Default, DuplicateHandling.RaisesException)));
    }

    [TestMethod]
    public void TestUnorderedAssertion()
    {
        Assert.Throws<InvalidDataException>(() => new List<int>(OrderedEnumeration<int>.WithDuplicateHandling([2, 1], Comparer<int>.Default, DuplicateHandling.RaisesException)));
    }

    private static IEnumerable<byte> FailBeforeYield<T>(bool bFail)
    {
        if (bFail) 
            throw new InvalidOperationException();
        
        yield break;
    }

    [TestMethod]
    public void TestOrderedEnumProperties()
    {
        var ordered = new OrderedEnumeration<byte>(Comparer<byte>.Default, FailBeforeYield<byte>(true));

        Assert.IsTrue(ReferenceEquals(Comparer<byte>.Default, ordered.Comparer));
        ordered.Comparer = new ReverseOrder<byte>(ordered.Comparer);
        Assert.IsTrue(ordered.Comparer is ReverseOrder<byte>);

        Assert.IsNull(ordered.Serializer);
        ordered.Serializer = PrimitiveSerializer.Byte;
        Assert.IsTrue(ReferenceEquals(ordered.Serializer, PrimitiveSerializer.Byte));

        Assert.AreEqual(0x10000, ordered.InMemoryLimit);
        Assert.AreEqual(10, ordered.InMemoryLimit = 10);

        Assert.AreEqual(DuplicateHandling.None, ordered.DuplicateHandling);
        Assert.AreEqual(DuplicateHandling.FirstValueWins, ordered.DuplicateHandling = DuplicateHandling.FirstValueWins);
    }

    [TestMethod]
    public void TestOrderedEnumDedup()
    {
        byte[] input = new byte[512];
        new Random().NextBytes(input);
		var ordered = new OrderedEnumeration<byte>(input)
		{
			InMemoryLimit = 10,
			DuplicateHandling = DuplicateHandling.FirstValueWins
		};

		int last = -1, count = 0;
        byte[] test = new List<byte>(ordered).ToArray();
        foreach (byte b in test)
        {
            count++;
            Assert.IsLessThan(b, last);
            last = b;
        }
        Assert.IsLessThanOrEqualTo(256, count);
    }

    [TestMethod]
    public void TestOrderedEnumPaginated()
    {
        byte[] input = new byte[512];
        new Random().NextBytes(input);
		var ordered = new OrderedEnumeration<byte>(input)
		{
			Serializer = PrimitiveSerializer.Byte,
			InMemoryLimit = 10,
			DuplicateHandling = DuplicateHandling.FirstValueWins
		};

		int last = -1, count = 0;
        byte[] test = new List<byte>(ordered).ToArray();
        foreach (byte b in test)
        {
            count++;
            Assert.IsLessThan(b, last);
            last = b;
        }
        Assert.IsLessThanOrEqualTo(256, count);
    }

    [TestMethod]
    public void TestOrderedEnumPaginatedCleanup()
    {
        byte[] input = new byte[512];
        new Random().NextBytes(input);
		var ordered = new OrderedEnumeration<byte>(input)
		{
			Serializer = PrimitiveSerializer.Byte,
			InMemoryLimit = 10,
			DuplicateHandling = DuplicateHandling.FirstValueWins
		};

		using var e = ordered.GetEnumerator();
		Assert.IsTrue(e.MoveNext());
	}
    
    [TestMethod]
    public void TestEnumTwiceFails()
    {
        var ordered = new OrderedEnumeration<byte>([]);
        using (var e = ordered.GetEnumerator())
            Assert.IsFalse(e.MoveNext());

        try
        {
            ((System.Collections.IEnumerable) ordered).GetEnumerator();
            Assert.Fail();
        }
        catch (InvalidOperationException) { }
    }

    [TestMethod]
    public void TestMergeEnumerations()
    {
        char[] x = "aeiou".ToCharArray();
        char[] y = "bcdfg".ToCharArray();
        char[] z = "ez".ToCharArray();

        var order = OrderedEnumeration<char>.Merge(x, y, z);
        Assert.AreEqual("abcdeefgiouz", new string(new List<char>(order).ToArray()));

        order = OrderedEnumeration<char>.Merge(Comparer<char>.Default, DuplicateHandling.LastValueWins, x, y, z);
        Assert.AreEqual("abcdefgiouz", new string(new List<char>(order).ToArray()));

        order = OrderedEnumeration<char>.Merge(Comparer<char>.Default, x, y);
        order = OrderedEnumeration<char>.WithDuplicateHandling(order, Comparer<char>.Default,
                                                               DuplicateHandling.FirstValueWins);
        Assert.AreEqual("abcdefgiou", new string(new List<char>(order).ToArray()));
    }

    [TestMethod]
    public void TestEnumInvalid()
    {
        var order = new OrderedEnumeration<byte>(new byte[1]);
        var e = ((System.Collections.IEnumerable)order).GetEnumerator();
        Assert.IsTrue(e.MoveNext());
        Assert.IsFalse(e.MoveNext());
        try
        {
            object val = e.Current;
            GC.KeepAlive(val);
            Assert.Fail();
        }
        catch (InvalidOperationException) { }

        try
        {
            e.Reset();
            Assert.Fail();
        }
        catch (NotSupportedException) { }
    }

    [TestMethod]
    public void TestOrderedKeyValuePairsMerge()
    {
        var x = new[] { new KeyValuePair<int, int>(1, 1) };
        var y = new[] { new KeyValuePair<int, int>(2, 2) };

        IEnumerator<KeyValuePair<int, int>> e = 
            OrderedKeyValuePairs<int, int>
            .Merge(new ReverseOrder<int>(Comparer<int>.Default), x, y)
            .GetEnumerator();

        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(2, e.Current.Key);
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(1, e.Current.Key);
        Assert.IsFalse(e.MoveNext());
    }

    [TestMethod]
    public void TestOrderedKeyValuePairsMergeOnDuplicate()
    {
        var x = new[] { new KeyValuePair<int, int>(1, 1) };
        var y = new[] { new KeyValuePair<int, int>(1, 2), new KeyValuePair<int, int>(2, 2) };

        IEnumerator<KeyValuePair<int, int>> e =
            OrderedKeyValuePairs<int, int>
            .Merge(Comparer<int>.Default, DuplicateHandling.FirstValueWins, x, y)
            .GetEnumerator();

        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(1, e.Current.Key);
        Assert.AreEqual(1, e.Current.Value);
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(2, e.Current.Key);
        Assert.AreEqual(2, e.Current.Value);
        Assert.IsFalse(e.MoveNext());
        
        e = OrderedKeyValuePairs<int, int>
            .Merge(Comparer<int>.Default, DuplicateHandling.LastValueWins, x, y)
            .GetEnumerator();

        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(1, e.Current.Key);
        Assert.AreEqual(2, e.Current.Value);
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(2, e.Current.Key);
        Assert.AreEqual(2, e.Current.Value);
        Assert.IsFalse(e.MoveNext());
    }

    [TestMethod]
    public void TestOrderedKeyValuePairsOverloads()
    {
        var e = Array.Empty<KeyValuePair<int, int>>();
        OrderedKeyValuePairs<int, int> ordered;

        ordered = new OrderedKeyValuePairs<int, int>(e);
        Assert.IsTrue(ordered.Comparer is KeyValueComparer<int, int>);
        Assert.IsTrue(ReferenceEquals(Comparer<int>.Default, ((KeyValueComparer<int, int>)ordered.Comparer).Comparer));
        Assert.AreEqual(DuplicateHandling.None, ordered.DuplicateHandling);
        Assert.AreEqual(0x10000, ordered.InMemoryLimit);
        Assert.IsNull(ordered.Serializer);

        ordered = new OrderedKeyValuePairs<int, int>(new ReverseOrder<int>(Comparer<int>.Default), e);
        Assert.IsTrue(ordered.Comparer is KeyValueComparer<int, int>);
        Assert.IsTrue(((KeyValueComparer<int, int>)ordered.Comparer).Comparer is ReverseOrder<int>);
        Assert.AreEqual(DuplicateHandling.None, ordered.DuplicateHandling);
        Assert.AreEqual(0x10000, ordered.InMemoryLimit);
        Assert.IsNull(ordered.Serializer);

        KeyValueSerializer<int,int> ser = new KeyValueSerializer<int,int>(PrimitiveSerializer.Int32, PrimitiveSerializer.Int32);
        ordered = new OrderedKeyValuePairs<int, int>(new ReverseOrder<int>(Comparer<int>.Default), e, ser);
        Assert.IsTrue(ordered.Comparer is KeyValueComparer<int, int>);
        Assert.IsTrue(((KeyValueComparer<int, int>)ordered.Comparer).Comparer is ReverseOrder<int>);
        Assert.AreEqual(DuplicateHandling.None, ordered.DuplicateHandling);
        Assert.AreEqual(0x10000, ordered.InMemoryLimit);
        Assert.AreEqual(ser, ordered.Serializer);

        ordered = new OrderedKeyValuePairs<int, int>(new ReverseOrder<int>(Comparer<int>.Default), e, ser, 42);
        Assert.IsTrue(ordered.Comparer is KeyValueComparer<int, int>);
        Assert.IsTrue(((KeyValueComparer<int, int>)ordered.Comparer).Comparer is ReverseOrder<int>);
        Assert.AreEqual(DuplicateHandling.None, ordered.DuplicateHandling);
        Assert.AreEqual(42, ordered.InMemoryLimit);
        Assert.AreEqual(ser, ordered.Serializer);

        ordered = new OrderedKeyValuePairs<int, int>(new ReverseOrder<int>(Comparer<int>.Default), e, PrimitiveSerializer.Int32, PrimitiveSerializer.Int32);
        Assert.IsTrue(ordered.Comparer is KeyValueComparer<int, int>);
        Assert.IsTrue(((KeyValueComparer<int, int>)ordered.Comparer).Comparer is ReverseOrder<int>);
        Assert.AreEqual(DuplicateHandling.None, ordered.DuplicateHandling);
        Assert.AreEqual(0x10000, ordered.InMemoryLimit);
        Assert.IsNotNull(ordered.Serializer);

        ordered = new OrderedKeyValuePairs<int, int>(new ReverseOrder<int>(Comparer<int>.Default), e, PrimitiveSerializer.Int32, PrimitiveSerializer.Int32, 42);
        Assert.IsTrue(ordered.Comparer is KeyValueComparer<int, int>);
        Assert.IsTrue(((KeyValueComparer<int, int>)ordered.Comparer).Comparer is ReverseOrder<int>);
        Assert.AreEqual(DuplicateHandling.None, ordered.DuplicateHandling);
        Assert.AreEqual(42, ordered.InMemoryLimit);
        Assert.IsNotNull(ordered.Serializer);
    }
}

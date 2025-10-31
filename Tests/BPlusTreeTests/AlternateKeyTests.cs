
using System;
using System.IO;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

[TestClass]
public class AlternateKeyTests
{
	[TestMethod]
	public void StringKeyReadWithReadonlySpanTest()
	{
		var fileName = Path.GetTempFileName();
		using (var tree = BPlusTree.Create(PrimitiveSerializer.String, PrimitiveSerializer.Int32, fileName))
		{
			tree.Add("one", 1);
			tree.Add("two", 2);
			tree.Add("three", 3);
			Assert.AreEqual(1, tree["one"]);
			Assert.AreEqual(2, tree["two"]);
			Assert.AreEqual(3, tree["three"]);

			var alternate = tree.GetAlternateLookup(new StringKeyAlternate());

			ReadOnlySpan<char> k3 = "three".AsSpan();
			Assert.AreEqual(3, alternate[k3]);

			ReadOnlySpan<char> k1 = "one".AsSpan();
			Assert.AreEqual(1, alternate[k1]);
			
			ReadOnlySpan<char> k2 = "two".AsSpan();
			Assert.AreEqual(2, alternate[k2]);
		}

		File.Delete(fileName);
	}
}

public class StringKeyAlternate : IAlternateComparer<ReadOnlySpan<char>, string>
{
	public int Compare(string x, ReadOnlySpan<char> y)
	{
		return x.AsSpan().CompareTo(y, StringComparison.OrdinalIgnoreCase);
	}
}


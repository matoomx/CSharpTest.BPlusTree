
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

			var alternate = tree.GetAlternateLookup(AlternateComparers.StringOrdinal);

			ReadOnlySpan<char> k = "three".AsSpan();
			Assert.AreEqual(3, alternate[k]);

			k = "one".AsSpan();
			Assert.AreEqual(1, alternate[k]);
			
			k = "two".AsSpan();
			Assert.AreEqual(2, alternate[k]);

			k = "Two".AsSpan();

			bool ex = false;
			try
			{
				var x = alternate[k];
				Assert.Fail("Expected KeyNotFoundException");
			}
			catch (IndexOutOfRangeException)
			{
				ex = true;
			}

			Assert.IsTrue(ex, "Expected KeyNotFoundException was not thrown");

			var alternate2 = tree.GetAlternateLookup(AlternateComparers.StringOrdinalIgnoreCase);

			Assert.AreEqual(2, alternate2[k]);
		}

		File.Delete(fileName);
	}
}


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
using System.Diagnostics;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

[TestClass]
public class TestBulkInsert
{
    protected static BPlusTree<int, string>.Options Options
    {
        get
        {
            return new BPlusTree<int, string>.Options(new PrimitiveSerializer(), new PrimitiveSerializer())
            {
                BTreeOrder = 4,
                LockingFactory = new IgnoreLockFactory(),
                EnableCount = true
			};
        }
    }

    static IEnumerable<KeyValuePair<int, string>> Set(IEnumerable<int> l, string v)
    {
        int last = -1;

        foreach (int i in l)
        {
            if(last < 0 || i > last)
                yield return new KeyValuePair<int, string>(i, v);
            last = i;
        }
    }
    
    [TestMethod]
    public void TestMergeSortedEnumerations1000()
    {
        for (int i = 0; i < 1000; i++)
            TestMergeSortedEnumerations();
    }

    private static IEnumerable<IEnumerable<KeyValuePair<int, string>>> CreateSets(int count, int size, Dictionary<int, string> expect)
    {
        int[][] sets = new int[count][];
        var r = new Random();

        for( int i=0; i < sets.Length; i++ )
        {
            sets[i] = new int[r.Next(size, size << 1)];
            for (int j = 0; j < sets[i].Length; j++)
            {
                sets[i][j] = r.Next(size << 4);
                expect[sets[i][j]] = i.ToString("n0");
            }

            MergeSort.Sort(sets[i]);
            yield return Set(sets[i], i.ToString("n0"));
        }
    }

    [TestMethod]
    public void TestMergeSortedEnumerations()
    {
        var test = new Dictionary<int, string>();
        var sets = new List<IEnumerable<KeyValuePair<int, string>>>(CreateSets(2, 100, test));
        
        foreach (var pair in OrderedKeyValuePairs<int, string>.Merge(Options.KeyComparer, DuplicateHandling.LastValueWins, sets.ToArray()))
        {
			Assert.IsTrue(test.TryGetValue(pair.Key, out string val));
			Assert.AreEqual(pair.Value, val);
            Assert.IsTrue(test.Remove(pair.Key));
        }

        Assert.IsEmpty(test);
    }

    [TestMethod]
    public void TestBulkInsertRandom()
    {
        for (int count = 0; count < 260; count++)
			TestMergeRandom(Options, 1, count);
    }

    [TestMethod]
    public void TestMergeInsertRandom()
    {
        for (int count = 0; count < 260; count += 7)
			TestMergeRandom(Options, 4, count);
    }

    [TestMethod]
    public void TestMergeSequenceInFile()
    {
        BPlusTree<int, string>.Options options = Options;
		using TempFile temp = new TempFile();
		options = Options;
		temp.Delete();
		//options.CreateFile = CreatePolicy.Always;
		//options.FileName = temp.TempPath;
		options.MaximumValueNodes = 14;
		options.MinimumValueNodes = 7;
		options.MaximumChildNodes = 6;
		options.MinimumChildNodes = 2;

		// Just to make sure we don't break some fencepost condition in the future
		for (int i = 0; i <= options.MaximumValueNodes * options.MaximumChildNodes + 1; i++)
			TestMergeSequenceInFile(options.Clone(), i);

		TestMergeSequenceInFile(options.Clone(), options.MaximumValueNodes * options.MaximumChildNodes * options.MaximumChildNodes);
		TestMergeSequenceInFile(options.Clone(), options.MaximumValueNodes * options.MaximumChildNodes * options.MaximumChildNodes + 1);
	}

	static void TestMergeSequenceInFile(BPlusTree<int, string>.Options options, int count)
    {
        var expected = new Dictionary<int, string>();

        for (int i = 0; i < count; i++)
            expected.Add(i + 1, i.ToString());

		using var tree = BPlusTree.Create(options);
		Assert.AreEqual(expected.Count, tree.BulkInsert(expected));
		VerifyDictionary(expected, tree);
	}

    [TestMethod]
    public void TestMergeRandomInFile()
    {
        var options = Options;
		using var temp = new TempFile();
		temp.Delete();
		options.CreateFile = CreatePolicy.Always;
		options.FileName = temp.TempPath;
		options.CalcBTreeOrder(4, 4);
		Stopwatch sw = Stopwatch.StartNew();

		var expected = TestMergeRandom(options, 2, 300);

		Trace.TraceInformation("Creating {0} nodes in {1}.", expected.Count, sw.Elapsed);
		sw = Stopwatch.StartNew();

		options = Options;
		options.CreateFile = CreatePolicy.Never;
		options.FileName = temp.TempPath;
		options.CalcBTreeOrder(4, 4);
		using (var tree = BPlusTree.Create(options))
			VerifyDictionary(expected, tree);

		Trace.TraceInformation("Verified {0} nodes in {1}.", expected.Count, sw.Elapsed);
	}

    public static Dictionary<int, string> TestMergeRandom(BPlusTree<int, string>.Options options, int nsets, int nsize)
    {
        var test = new Dictionary<int, string>();
        var sets = new List<IEnumerable<KeyValuePair<int, string>>>(CreateSets(nsets, nsize, test)).ToArray();

        using (var tree = BPlusTree.Create(options))
        {
            foreach(IEnumerable<KeyValuePair<int, string>> set in sets)
                tree.BulkInsert(set, new BulkInsertOptions { DuplicateHandling = DuplicateHandling.LastValueWins });

            VerifyDictionary(test, tree);

            tree.UnloadCache();
            tree.Add(int.MaxValue, "max");
            tree.Remove(int.MaxValue);

            VerifyDictionary(test, tree);
        }

        return test;
    }

    private static void VerifyDictionary(Dictionary<int, string> expected, BPlusTree<int, string> tree)
    {
       // tree.Validate();
        //tree.EnableCount();

        var test = new Dictionary<int, string>(expected);
        var pairs = new List<KeyValuePair<int, string>>(test);

        foreach (var pair in tree)
        {
            Assert.IsTrue(test.TryGetValue(pair.Key, out var val));
            Assert.AreEqual(pair.Value, val);
            Assert.IsTrue(test.Remove(pair.Key));
        }
        Assert.IsEmpty(test);
        test = null;
        Assert.IsNull(test);
        Assert.AreEqual(pairs.Count, tree.Count);

        foreach (var pair in pairs)
        {
            Assert.IsTrue(tree.TryGetValue(pair.Key, out var val));
            Assert.AreEqual(pair.Value, val);
        }
    }

    [TestMethod]
    public void TestBulkInsertSorted()
    {
        var test = new Dictionary<int, string>();
        var sets = new List<IEnumerable<KeyValuePair<int, string>>>(CreateSets(1, 1000, test)).ToArray();

		using var tree = BPlusTree.Create(Options);
		tree.BulkInsert(
			new OrderedKeyValuePairs<int, string>(sets[0]),
			new BulkInsertOptions { DuplicateHandling = DuplicateHandling.LastValueWins, InputIsSorted = true });

		VerifyDictionary(test, tree);
	}

    [TestMethod]
    public void TestReplaceContents()
    {
        var test = new Dictionary<int, string>();
        var sets = new List<IEnumerable<KeyValuePair<int, string>>>(CreateSets(1, 1000, test)).ToArray();

		using var tree = BPlusTree.Create(Options);
		tree.BulkInsert(
			new OrderedKeyValuePairs<int, string>(sets[0]),
			new BulkInsertOptions { DuplicateHandling = DuplicateHandling.LastValueWins, InputIsSorted = true });

		VerifyDictionary(test, tree);

		// Use bulk insert to overwrite the contents of tree
		test = new Dictionary<int, string>();
		sets = new List<IEnumerable<KeyValuePair<int, string>>>(CreateSets(1, 100, test)).ToArray();

		tree.BulkInsert(
			new OrderedKeyValuePairs<int, string>(sets[0]),
			new BulkInsertOptions
			{
				CommitOnCompletion = false,
				InputIsSorted = true,
				ReplaceContents = true,
				DuplicateHandling = DuplicateHandling.RaisesException,
			}
		);

		VerifyDictionary(test, tree);
	}
}

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
using System.IO;
using System.Collections.Generic;
using CSharpTest.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;


[TestClass]
public class TestBackupAndRecovery
{
	static BPlusTree<Guid, TestInfo>.Options GetOptions(TempFile temp)
    {
        var options = new BPlusTree<Guid, TestInfo>.Options(PrimitiveSerializer.Guid, new TestInfoSerializer());
        options.CalcBTreeOrder(Marshal.SizeOf<Guid>(), 4096);
        options.CreateFile = CreatePolicy.IfNeeded;
        options.FileName = temp.TempPath;
        return options;
    }

    static void Insert(BPlusTree<Guid, TestInfo> tree, IDictionary<Guid, TestInfo> testdata, int threads, int count, TimeSpan wait)
    {
		using var work = new WorkQueue<IEnumerable<KeyValuePair<Guid, TestInfo>>>(tree.AddRange, threads);
		foreach (var set in TestInfo.CreateSets(threads, count, testdata))
			work.Enqueue(set);
		work.Complete(true, wait == TimeSpan.MaxValue ? Timeout.Infinite : (int)Math.Min(int.MaxValue, wait.TotalMilliseconds));
	}

    [TestMethod]
    public void TestRecoveryOnNewWithAsyncLog()
    {
		using var temp = new TempFile();
		var options = GetOptions(temp);
		options.SetLogFile(new TransactionLog<Guid, TestInfo>(new TransactionLogOptions<Guid, TestInfo>(temp.Info.FullName + ".tlog", options.KeySerializer, options.ValueSerializer)));
		TestRecoveryOnNew(options, 100, 0);
	}

    [TestMethod]
    public void TestRestoreLargeLog()
    {
		using TempFile savelog = new TempFile();
		using TempFile temp = new TempFile();

		var options = GetOptions(temp);
		options.FileBlockSize = 512;
		options.CalcBTreeOrder(Marshal.SizeOf<Guid>(), 4096);
		options.StoragePerformance = StoragePerformance.Fastest;
		options.SetLogFile(new TransactionLog<Guid, TestInfo>(new TransactionLogOptions<Guid, TestInfo>(Path.ChangeExtension(temp.TempPath, ".tlog") , options.KeySerializer, options.ValueSerializer)));
		options.EnableCount = true;

		//Now recover...
		Dictionary<Guid, TestInfo> first = new Dictionary<Guid, TestInfo>();
		Dictionary<Guid, TestInfo> sample;

		using (var tree = new BPlusTree<Guid, TestInfo>(options))
		{
			Insert(tree, first, 1, 100, TimeSpan.FromMinutes(1));
			tree.Commit();

			Assert.AreEqual(100, tree.Count);

			sample = new Dictionary<Guid, TestInfo>(first);
			Insert(tree, sample, 7, 5000, TimeSpan.FromMinutes(1));

			Assert.AreEqual(35100, tree.Count);

			for (int i = 0; i < 1; i++)
			{
				foreach (var rec in tree)
				{
					var value = rec.Value;
					value.UpdateCount++;
					value.ReadCount++;
					tree[rec.Key] = value;
				}
			}

			File.Copy(Path.ChangeExtension(temp.TempPath, ".tlog"), savelog.TempPath, true);
			tree.Rollback();

			TestInfo.AssertEquals(first, tree);
		}

		//file still has initial committed data
		TestInfo.AssertEquals(first, BPlusTree.EnumerateFile(options));

		File.Copy(savelog.TempPath, Path.ChangeExtension(temp.TempPath, ".tlog"), true);

		//restore the log and verify all data.
		options.SetLogFile(new TransactionLog<Guid, TestInfo>(new TransactionLogOptions<Guid, TestInfo>(Path.ChangeExtension(temp.TempPath, ".tlog"), options.KeySerializer, options.ValueSerializer)));
		using (var tree = new BPlusTree<Guid, TestInfo>(options))
		{
			TestInfo.AssertEquals(sample, tree);
		}

		//file still has initial committed data
		TestInfo.AssertEquals(sample, BPlusTree.EnumerateFile(options));
	}

    [TestMethod]
    public void TestRecoveryOnExistingWithAsyncLog()
    {
		using TempFile temp = new TempFile();
		var options = GetOptions(temp);
		options.SetLogFile(new TransactionLog<Guid, TestInfo>(
			new TransactionLogOptions<Guid, TestInfo>( temp.Info.FullName + ".tlog",options.KeySerializer, options.ValueSerializer )
			{ FileOptions = FileOptions.Asynchronous }));
		TestRecoveryOnExisting(options, 100, 0);
	}

    [TestMethod]
    public void TestRecoveryOnNew()
    {
		using TempFile temp = new TempFile();
		var options = GetOptions(temp);
		TestRecoveryOnNew(options, 10, 0);
	}

    [TestMethod]
    public void TestRecoveryOnExisting()
    {
		using var temp = new TempFile();
        var options = GetOptions(temp);
		TestRecoveryOnExisting(options, 10, 0);
	}


    [TestMethod]
    public void TestRecoveryOnNewLargeOrder()
    {
		using var temp = new TempFile();
		var options = GetOptions(temp);
		options.MaximumValueNodes = 255;
		options.MinimumValueNodes = 100;
		options.SetLogFile(new TransactionLog<Guid, TestInfo>(
			new TransactionLogOptions<Guid, TestInfo>(Path.ChangeExtension(temp.Info.FullName, ".tlog"),
				options.KeySerializer,
				options.ValueSerializer
				)
			{ FileOptions = FileOptions.None } /* no-write through */
			));
		TestRecoveryOnNew(options, 100, 10000);
	}

    [TestMethod]
    public void TestRecoveryOnExistingLargeOrder()
    {
		using var temp = new TempFile();
		var options = GetOptions(temp);
		options.MaximumValueNodes = 255;
		options.MinimumValueNodes = 100;
		options.EnableCount = true;
		options.SetLogFile(new TransactionLog<Guid, TestInfo>(
			new TransactionLogOptions<Guid, TestInfo>(Path.ChangeExtension(temp.Info.FullName, ".tlog"),
				options.KeySerializer,
				options.ValueSerializer
				)
			{ FileOptions = FileOptions.None } /* no-write through */
			));
		TestRecoveryOnExisting(options, 100, ushort.MaxValue);
	}

	static void TestRecoveryOnNew(BPlusTree<Guid, TestInfo>.Options options, int count, int added)
    {
        BPlusTree<Guid, TestInfo> tree = null;
        var temp = TempFile.Attach(options.FileName);
        Dictionary<Guid, TestInfo> data = new Dictionary<Guid, TestInfo>();
        try
        {
            temp.Delete();
			options.CommitOnDispose = false;
			tree = BPlusTree.Create(options);
            using (var log = EnsureLogFile(options))
            {
                Insert(tree, data, Environment.ProcessorCount, count, TimeSpan.MaxValue);
                //Add extra data...
                AppendToLog(log, TestInfo.Create(added, data));
            }            
            //No data... yet...
            using(TempFile testempty = TempFile.FromCopy(options.FileName))
            {
                var testoptions = options.Clone();
                testoptions.SetLogFile(null);
                testoptions.FileName = testempty.TempPath;
				testoptions.EnableCount = true;

				using var empty = BPlusTree.Create(testoptions);
				Assert.AreEqual(0, empty.Count);
			}
			tree.Dispose();
			tree = null;

			//Now recover...
			using var recovered = BPlusTree.Create(options);
			TestInfo.AssertEquals(data, recovered);
		}
        finally
        {
            temp.Dispose();
            tree?.Dispose();
        }
    }


	private static ITransactionLog<TKey, TValue> EnsureLogFile<TKey, TValue>(BPlusTree<TKey, TValue>.Options options)
	{
		if (options.GetLogFile() == null && options.FileName != null)
			options.SetLogFile(new TransactionLog<TKey, TValue>(new TransactionLogOptions<TKey, TValue>(Path.ChangeExtension(options.FileName, ".tlog"), options.KeySerializer, options.ValueSerializer)));
		
		return options.GetLogFile();
	}

	static void TestRecoveryOnExisting(BPlusTree<Guid, TestInfo>.Options options, int count, int added)
    {
        var temp = TempFile.Attach(options.FileName);
		BPlusTree<Guid, TestInfo> tree = null;
		Dictionary<Guid, TestInfo> dataFirst, data = new Dictionary<Guid, TestInfo>();
        try
        {
            temp.Delete();
			Assert.IsNotNull(EnsureLogFile(options));

            using (tree = new BPlusTree<Guid, TestInfo>(options))
            {
                Insert(tree, data, 1, 100, TimeSpan.MaxValue);
                TestInfo.AssertEquals(data, tree);
            }

            // All data commits to output file
            Assert.IsTrue(temp.Exists);
            TestInfo.AssertEquals(data, BPlusTree.EnumerateFile(options));

            dataFirst = new Dictionary<Guid, TestInfo>(data);
            DateTime modified = temp.Info.LastWriteTimeUtc;
			options.CommitOnDispose = false;
			tree = new BPlusTree<Guid, TestInfo>(options);
			using (var log = options.GetLogFile())
			{
				Insert(tree, data, Environment.ProcessorCount, count, TimeSpan.MaxValue);
				//Add extra data...
				AppendToLog(log, TestInfo.Create(added, data));
			}

			tree.Dispose();
			tree = null;

			//Now recover...
			options.StoragePerformance = StoragePerformance.Default;
			using var recovered = new BPlusTree<Guid, TestInfo>(options);
			TestInfo.AssertEquals(data, recovered);
		}
        finally
        {
            temp?.Dispose();
            tree?.Dispose();
        }
    }

    private static void AppendToLog(ITransactionLog<Guid, TestInfo> log, IEnumerable<KeyValuePair<Guid, TestInfo>> keyValuePairs)
    {
		using var items = keyValuePairs.GetEnumerator();
		bool more = items.MoveNext();
		while (more)
		{
			var tx = log.BeginTransaction();
			int count = 1000;
			do
			{
				log.AddValue(ref tx, items.Current.Key, items.Current.Value);
				more = items.MoveNext();
			} while (more && --count > 0);

			log.CommitTransaction(ref tx);
		}
	}
}

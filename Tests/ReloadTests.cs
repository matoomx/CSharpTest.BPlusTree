using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using CSharpTest.Collections.Generic;

namespace BPlusTreeTests;

[TestClass]
public sealed class ReloadTests
{

    [TestMethod]
    public void TestReadme()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.String, PrimitiveSerializer.DateTime);	
        options.CalcBTreeOrder(16, 24);
        options.CreateFile = CreatePolicy.Always;
        options.FileName = Path.GetTempFileName();
        using (var tree = BPlusTree.Create(options))
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
                tree.Add(file.FullName, file.LastWriteTimeUtc);
        }
        options.CreateFile = CreatePolicy.Never;
        using (var tree = BPlusTree.Create(options))
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
            {
                if (!tree.TryGetValue(file.FullName, out DateTime cmpDate))
                    Console.WriteLine("New file: {0}", file.FullName);
                else if (cmpDate != file.LastWriteTimeUtc)
                    Console.WriteLine("Modified: {0}", file.FullName);
                tree.Remove(file.FullName);
            }
            foreach (var item in tree)
                Console.WriteLine("Removed: {0}", item.Key);
        }
    }

    [TestMethod]
    public void BasicTest()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.String, PrimitiveSerializer.Int32);
        options.CalcBTreeOrder(16, 24);
        options.CreateFile = CreatePolicy.Always;
        options.FileName = Path.GetTempFileName();
        using (var tree = BPlusTree.Create(options))
        {
            tree.Add("A", 1);
            tree.Add("B", 2);
            tree.Add("C", 3);
        }
        options.CreateFile = CreatePolicy.Never;
        using (var tree = BPlusTree.Create(options))
        {
            Assert.AreEqual(1, tree["A"]);
            Assert.AreEqual(2, tree["B"]);
            Assert.AreEqual(3, tree["C"]);
        }
    }

    [TestMethod]
    public void Remove()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.String, PrimitiveSerializer.Int32);
        options.CalcBTreeOrder(16, 24);
        options.CreateFile = CreatePolicy.Always;
        options.FileName = Path.GetTempFileName();
        options.EnableCount = true;
        using (var tree = BPlusTree.Create(options))
        {
            tree.Add("A", 1);
            tree.Add("B", 2);
            tree.Add("C", 3);
            Assert.AreEqual(3, tree.Count);
            tree.Remove("B");
            Assert.AreEqual(2, tree.Count);
        }
        options.CreateFile = CreatePolicy.Never;
        using (var tree = BPlusTree.Create(options))
        {
            Assert.AreEqual(2, tree.Count);
            Assert.AreEqual(1, tree["A"]);
            Assert.AreEqual(3, tree["C"]);
        }
    }

    [TestMethod]
    public void CountTest()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.String, PrimitiveSerializer.Int32);
        options.EnableCount = true;
        options.CalcBTreeOrder(16, 24);
        options.CreateFile = CreatePolicy.Always;
        options.FileName = Path.GetTempFileName();
        using (var tree = BPlusTree.Create(options))
        {
            tree.Add("A", 1);
            tree.Add("B", 2);
            tree.Add("C", 3);
            Assert.AreEqual(3, tree.Count);
        }
        options.CreateFile = CreatePolicy.Never;
        using (var tree = BPlusTree.Create(options))
        {
            Assert.AreEqual(3, tree.Count);
        }
    }

    [TestMethod]
    public void BulkAdd_1_000_000_and_Reload()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.Int64, PrimitiveSerializer.String);
        options.CalcBTreeOrder(16, 24);
        options.CreateFile = CreatePolicy.Always;
        options.FileName = Path.GetTempFileName();
        options.StoragePerformance = StoragePerformance.Fastest;
        using (var tree = BPlusTree.Create(options))
        {
            tree.BulkInsert(Enumerable.Range(1, 1_000_000).Select(i => new KeyValuePair<long, string>(i, i.ToString())), new BulkInsertOptions { InputIsSorted = true});
        }
        options.CreateFile = CreatePolicy.Never;
        options.EnableCount = true;

        using (var tree = new BPlusTree<long, string>(options))
        {
            Assert.AreEqual(1_000_000, tree.Count);
        }
    }

    [TestMethod]
    public void Add_100_000_and_Reload()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.Int64, PrimitiveSerializer.String);
        options.CalcBTreeOrder(16, 24);
        options.CreateFile = CreatePolicy.Always;
        options.FileName = Path.GetTempFileName();
        options.StoragePerformance = StoragePerformance.Fastest;
        //options.LockingFactory = new CSharpTest.Collections.Generic.LockFactory<CSharpTest.Collections.Generic.SimpleReadWriteLocking>();
        using (var tree = BPlusTree.Create(options))
        {
            foreach (var kvp in Enumerable.Range(1, 100_000).Select(i => new KeyValuePair<long, string>(i, i.ToString())))
                tree.Add(kvp.Key, kvp.Value);
        }
        options.CreateFile = CreatePolicy.Never;
        options.EnableCount = true;
        using (var tree = new BPlusTree<long, string>(options))
        {
            Assert.AreEqual(100_000, tree.Count);
        }
    }


    [TestMethod]
    public void ParallelAdd_100_000_and_Reload()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.Int64, PrimitiveSerializer.String);
        options.FileName = Path.GetTempFileName();
        options.CreateFile = CreatePolicy.Always;
        options.StoragePerformance = StoragePerformance.Fastest;
        options.CalcBTreeOrder(16, 24);

        int concurrent = 0;
        int maxConcurrent = 0;
        using (var tree = new BPlusTree<long, string>(options))
        {
            Parallel.ForEach(Partitioner.Create(1, 100_001), range =>
            {
                var max = Interlocked.Increment(ref concurrent);

                do
                {
                    max = maxConcurrent;
                    if (max >= concurrent)
                        break;
                }
                while (Interlocked.CompareExchange(ref maxConcurrent, concurrent, max) != max);


                foreach (var kvp in Enumerable.Range(range.Item1, range.Item2 - range.Item1).Select(i => new KeyValuePair<long, string>(i, i.ToString())))
                    tree.Add(kvp.Key, kvp.Value);
                Interlocked.Decrement(ref concurrent);
            });
        }

        Console.WriteLine("Max Concurrent: {0}", maxConcurrent);

        options.CreateFile = CreatePolicy.Never;
        options.EnableCount = true;
        using (var tree = new BPlusTree<long, string>(options))
        {
            Assert.AreEqual(100_000, tree.Count);
        }
    }

    [TestMethod]
    public void Duplicate()
    {
        var options = BPlusTree.CreateOptions(PrimitiveSerializer.String, PrimitiveSerializer.Int32);
        options.CalcBTreeOrder(16, 24);
        options.CreateFile = CreatePolicy.Always;
        options.FileName = Path.GetTempFileName();
        options.EnableCount = true;
        using var tree = new BPlusTree<string, int>(options)
        {
            { "A", 1 },
            { "B", 2 }
        };
        Assert.Throws<DuplicateKeyException>( () => tree.Add("B", 3));
    }
}

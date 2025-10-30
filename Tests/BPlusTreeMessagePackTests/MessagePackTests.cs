using System.IO;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace BPlusTreeTestsMessagePack;

[TestClass]
public class MessagePackTests
{ 
	[TestMethod]
	public void TestMessagePackValue()
	{
		var dataFile = Path.GetTempFileName();
		var serializer = new MessagePackSerializer<Sample>();

		using (var tree = BPlusTree.Create(PrimitiveSerializer.Int64, serializer, dataFile))
		{
			tree.Add(1, new Sample { Name = "John", Age = 25 });
			tree.Add(2, new Sample { Name = "Ann", Age = 26 });
			tree.Add(3, new Sample { Name = "Jack", Age = 36 });
		}

		using (var tree = BPlusTree.Create(PrimitiveSerializer.Int64, serializer, dataFile))
		{
			Assert.AreEqual("John",tree[1].Name);
			Assert.AreEqual(25, tree[1].Age);
			Assert.AreEqual("Ann", tree[2].Name);
			Assert.AreEqual(26, tree[2].Age);
			Assert.AreEqual("Jack", tree[3].Name);
			Assert.AreEqual(36, tree[3].Age);
		}

		File.Delete(dataFile);
	}

	[TestMethod]
	public void TestMessagePackKey()
	{
		var dataFile = Path.GetTempFileName();
		var keySerializer = new MessagePackSerializer<SampleKey>();
		var vauleSerializer = new MessagePackSerializer<Sample>();
		var comparer = new SampleKeyComparer();

		using (var tree = BPlusTree.Create(keySerializer, vauleSerializer, comparer, dataFile))
		{
			tree.Add(new SampleKey { KeyPart1 = 1,KeyPart2 = 1 } , new Sample { Name = "John", Age = 25 });
			tree.Add(new SampleKey { KeyPart1 = 1, KeyPart2 = 2 }, new Sample { Name = "Ann", Age = 26 });
			tree.Add(new SampleKey { KeyPart1 = 3, KeyPart2 = 1 }, new Sample { Name = "Jack", Age = 36 });
		}

		using (var tree = BPlusTree.Create(keySerializer, vauleSerializer, comparer, dataFile))
		{
			var key = new SampleKey { KeyPart1 = 1, KeyPart2 = 1 };

			Assert.AreEqual("John", tree[key].Name);
			Assert.AreEqual(25, tree[key].Age);
			key = new SampleKey { KeyPart1 = 1, KeyPart2 = 2 };
			Assert.AreEqual("Ann", tree[key].Name);
			Assert.AreEqual(26, tree[key].Age);
			key = new SampleKey { KeyPart1 = 3, KeyPart2 = 1};
			Assert.AreEqual("Jack", tree[key].Name);
			Assert.AreEqual(36, tree[key].Age);
		}

		File.Delete(dataFile);
	}
}

using System.IO;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace BPlusTreeTestsMemoryPack;


[TestClass]
public class MemoryPackTests
{ 

	[TestMethod]
	public void TestMemoryPackSerializer()
	{
		var dataFile = Path.GetTempFileName();

		using (var tree = BPlusTree.Create(PrimitiveSerializer.Int64, new MemoryPackSerializer<Sample>(), dataFile))
		{
			tree.Add(1, new Sample { Name = "John", Age = 25 });
			tree.Add(2, new Sample { Name = "Ann", Age = 26 });
			tree.Add(3, new Sample { Name = "Jack", Age = 36 });
		}

		using (var tree = BPlusTree.Create(PrimitiveSerializer.Int64, new MemoryPackSerializer<Sample>(), dataFile))
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
}

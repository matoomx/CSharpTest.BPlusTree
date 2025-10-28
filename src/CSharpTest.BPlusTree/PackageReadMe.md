CSharpTest.BPlusTree
=======================

A B+Tree implementation in C# with an IDictionary<TKey,TValue> interface. Originally forked from CSharpTest.Net.Collections but cleared of most code not relevant to BPlusTree.

This fork has a modified serialization interface based on System.Buffers and a new storage implementation bases on RandomAccess. The storage format is not compatible with the original implementation.

### Example ###
```
var dataFile = Path.GetTempFileName();
var tempDir = new DirectoryInfo(Path.GetTempPath());

//Create a BPlusTree with with all temp files and when they where updated and use dataFile as storage. 
using (var tree = BPlusTree.Create(PrimitiveSerializer.String, PrimitiveSerializer.DateTime, dataFile))
{
	foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
		tree.Add(file.FullName, file.LastWriteTimeUtc);
}

//Create a new BPlusTree with the previously created dataFile and check what files have changed.
using (var tree = BPlusTree.Create(PrimitiveSerializer.String, PrimitiveSerializer.DateTime, dataFile))
{
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
```

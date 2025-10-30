CSharpTest.BPlusTree.MessagePack
=======================

A MessagePack serializer implementation for CSharpTest.BPlusTree.

### Example ###
```csharp
using MessagePack;

[MessagePackObject]
public class Sample
{
	[Key(0)]
	public string Name { get; init; }
	[Key(1)]
	public int Age { get; init; }
}
```

```csharp
using CSharpTest.BPlusTree;

var dataFile = System.IO.Path.GetTempFileName();
var serializer = new MessagePackSerializer<Sample>();

using (var tree = BPlusTree.Create(PrimitiveSerializer.Int64, serializer, dataFile))
{
	tree.Add(1, new Sample { Name = "John", Age = 25 });
	tree.Add(2, new Sample { Name = "Ann", Age = 26 });
	tree.Add(3, new Sample { Name = "Jack", Age = 36 });
}
```

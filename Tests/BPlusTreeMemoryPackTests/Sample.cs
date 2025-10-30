using MemoryPack;

namespace BPlusTreeTestsMemoryPack;

[MemoryPackable]
public partial class Sample
{
	public string Name { get; init; }
	public int Age { get; init; }
}

using MessagePack;

namespace BPlusTreeTestsMessagePack;

[MessagePackObject]
public class Sample
{
	[Key(0)]
	public string Name { get; init; }
	[Key(1)]
	public int Age { get; init; }
}

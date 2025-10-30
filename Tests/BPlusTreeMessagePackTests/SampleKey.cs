using MessagePack;
using System.Collections.Generic;

namespace BPlusTreeTestsMessagePack;

[MessagePackObject]
public class SampleKey
{
	[Key(0)]
	public int KeyPart1 { get; init; }
	[Key(1)]
	public int KeyPart2 { get; init; }
}

public class SampleKeyComparer :IComparer<SampleKey>
{
	public int Compare(SampleKey x, SampleKey y)
	{
		int result = x.KeyPart1.CompareTo(y.KeyPart1);

		if (result == 0)
			result = x.KeyPart2.CompareTo(y.KeyPart2);

		return result;
}
}

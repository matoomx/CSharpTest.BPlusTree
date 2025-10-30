using System.Buffers;

namespace CSharpTest.Collections.Generic;

public sealed class MemoryPackSerializer<T>: SizePrefixedSerializerBase<T>
{
	protected override void Serialize(T value, IBufferWriter<byte> writer)
	{
		MemoryPack.MemoryPackSerializer.Serialize(writer, value);
	}

	protected override T Deserialize(ReadOnlySequence<byte> data)
	{
		return MemoryPack.MemoryPackSerializer.Deserialize<T>(data);
	}
}
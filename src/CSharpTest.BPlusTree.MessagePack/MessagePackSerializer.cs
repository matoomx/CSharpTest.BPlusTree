using System.Buffers;

namespace CSharpTest.Collections.Generic;

public sealed class MessagePackSerializer<T>: SizePrefixedSerializerBase<T>
{
	protected override void Serialize(T value, IBufferWriter<byte> writer)
	{
		MessagePack.MessagePackSerializer.Serialize(writer, value);
	}

	protected override T Deserialize(ReadOnlySequence<byte> data)
	{
		return MessagePack.MessagePackSerializer.Deserialize<T>(data);
	}
}
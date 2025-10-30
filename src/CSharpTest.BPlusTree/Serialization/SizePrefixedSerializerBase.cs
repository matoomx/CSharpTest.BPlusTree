using System;
using System.Buffers;
using System.Buffers.Binary;

namespace CSharpTest.Collections.Generic;

/// <summary> Provides serialization for a type with a int32 size header</summary>
public abstract class SizePrefixedSerializerBase<T>: ISerializer<T>
{
	public void WriteTo(T value, IBufferWriter<byte> writer)
	{
		if (writer is not SerializeStream stream)
			throw new InvalidOperationException("SizePrefixedSerializer requires a SerializeStream as the IBufferWriter<byte>.");

		var sizeHeader = stream.GetSpan(4);
		stream.Advance(4);
		var start = stream.Position;
		Serialize(value, writer);
		BinaryPrimitives.WriteInt32LittleEndian(sizeHeader, (int)(stream.Position - start));
	}

	protected abstract void Serialize(T value, IBufferWriter<byte> writer);

	public T ReadFrom(ReadOnlySequence<byte> data, ref SequencePosition position)
	{
		var slice = data.Slice(position);
		if (slice.Length < 4)
			throw new InvalidOperationException("Not enough data to read size prefix.");

		int size;
		var sizeHeader = slice.Slice(0, 4);

		if (sizeHeader.IsSingleSegment)
		{
			size = BinaryPrimitives.ReadInt32LittleEndian(sizeHeader.First.Span);
		} 
		else
		{
			Span<byte> sizeBuffer = stackalloc byte[4];
			sizeHeader.CopyTo(sizeBuffer);
			size = BinaryPrimitives.ReadInt32LittleEndian(sizeBuffer);
		}

		slice = slice.Slice(4);
		if (slice.Length < size)
			throw new InvalidOperationException("Not enough data to read the serialized object.");
		
		position = data.GetPosition(4 + size, position);

		return Deserialize(slice.Slice(0, size));
	}

	protected abstract T Deserialize(ReadOnlySequence<byte> data);
}

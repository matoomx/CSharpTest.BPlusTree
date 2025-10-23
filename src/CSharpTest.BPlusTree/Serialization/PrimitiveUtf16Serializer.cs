using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

internal sealed class PrimitiveUtf16Serializer : ISerializer<string>
{
	void ISerializer<string>.WriteTo(string value, IBufferWriter<byte> stream)
	{
		if (value == null)
		{
			PrimitiveSerializer.Int32.WriteTo(int.MinValue, stream);
		}
		else
		{
			var bytes = MemoryMarshal.Cast<char, byte>(value);
			var target = stream.GetSpan(bytes.Length + 4);
			BinaryPrimitives.WriteInt32LittleEndian(target, value.Length);
			bytes.CopyTo(target.Slice(4));
			stream.Advance(bytes.Length + 4);
		}
	}

	string ISerializer<string>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
	{
		int sz = PrimitiveSerializer.Int32.ReadFrom(stream, ref position);
		if (sz == 0) return string.Empty;
		if (sz == int.MinValue)
			return null;

		var res = string.Create(sz, (stream, position), (span, state) =>
		{
			var buffer = MemoryMarshal.Cast<char, byte>(span);
			if (!state.stream.TryRead(ref state.position, buffer))
				throw new InvalidDataException("Not enough data to read a String");
		});

		position = stream.GetPosition(sz * 2, position);

		return res;
	}
}
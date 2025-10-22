using System;
using System.Buffers;

namespace CSharpTest.Collections.Generic;

/// <summary> Provides serialization for a type </summary>
public interface ISerializer<T>
{
	/// <summary> Writes the object to the stream </summary>
	void WriteTo(T value, IBufferWriter<byte> writer);
	/// <summary> Reads the object from a stream </summary>
	T ReadFrom(ReadOnlySequence<byte> data, ref SequencePosition position);
}

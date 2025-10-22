#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Provides simple implementations of ISerializer&lt;T> for the primitive .Net types.
/// </summary>
public sealed class PrimitiveSerializer :
    ISerializer<string>,
    ISerializer<bool>,
    ISerializer<byte>,
    ISerializer<sbyte>,
    ISerializer<byte[]>,
    ISerializer<char>,
    ISerializer<DateTime>,
    ISerializer<TimeSpan>,
    ISerializer<short>,
	ISerializer<ushort>,
    ISerializer<int>,
	ISerializer<uint>,
	ISerializer<long>,
    ISerializer<ulong>,
    ISerializer<double>,
    ISerializer<float>,
    ISerializer<Guid>,
    ISerializer<IntPtr>,
    ISerializer<UIntPtr>
{
    /// <summary> Gets a singleton of the PrimitiveSerializer </summary>
    public static readonly PrimitiveSerializer Instance = new PrimitiveSerializer();
    /// <summary> Gets a typed version of the PrimitiveSerializer</summary>
    public static readonly ISerializer<string> String = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<bool> Boolean = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<byte> Byte = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<sbyte> SByte = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<byte[]> Bytes = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<char> Char = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<DateTime> DateTime = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<TimeSpan> TimeSpan = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<short> Int16 = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<ushort> UInt16 = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<int> Int32 = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<uint> UInt32 = Instance;
	/// <summary> Gets a typed version of the PrimitiveSerializer </summary>
	public static readonly ISerializer<long> Int64 = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<ulong> UInt64 = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<double> Double = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<float> Float = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<Guid> Guid = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<IntPtr> IntPtr = Instance;
    /// <summary> Gets a typed version of the PrimitiveSerializer </summary>
    public static readonly ISerializer<UIntPtr> UIntPtr = Instance;


    #region ISerializer<bool> Members

    void ISerializer<bool>.WriteTo(bool value, IBufferWriter<byte> stream)
    {
		const byte bTrue = 1;
		const byte bFalse = 0;
		stream.Write(value ? bTrue : bFalse);
	}

    bool ISerializer<bool>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return stream.ReadByte(ref position) == 1;
    }

    #endregion
    #region ISerializer<byte> Members

    void ISerializer<byte>.WriteTo(byte value, IBufferWriter<byte> stream)
    {
        stream.Write(value);
    }

    byte ISerializer<byte>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return stream.ReadByte(ref position);
    }

    #endregion
    #region ISerializer<sbyte> Members

    void ISerializer<sbyte>.WriteTo(sbyte value, IBufferWriter<byte> stream)
    {
        stream.Write(unchecked((byte)value));
    }

    sbyte ISerializer<sbyte>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return unchecked((sbyte)stream.ReadByte(ref position));
    }

	#endregion
	#region ISerializer<byte[]> Members

	void ISerializer<byte[]>.WriteTo(byte[] value, IBufferWriter<byte> stream)
	{
		if (value == null)
			Int32.WriteTo(int.MinValue, stream);
		else
		{
			Int32.WriteTo(value.Length, stream);
			stream.Write(value);
		}
	}
	byte[] ISerializer<byte[]>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
	{
		int sz = Int32.ReadFrom(stream, ref position);
		if (sz == int.MinValue)
			return null;

		byte[] bytes = new byte[sz];

		if (!stream.TryRead(ref position, bytes))
			throw new InvalidDataException();

		return bytes;
	}

	#endregion
	#region ISerializer<char> Members

	void ISerializer<char>.WriteTo(char value, IBufferWriter<byte> stream)
    {
        UInt16.WriteTo(value, stream);
    }

    char ISerializer<char>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return unchecked((char)UInt16.ReadFrom(stream, ref position));
    }

    #endregion
    #region ISerializer<DateTime> Members

    void ISerializer<DateTime>.WriteTo(DateTime value, IBufferWriter<byte> stream)
    {
        ((ISerializer<long>)this).WriteTo(value.ToBinary(), stream);
    }

    DateTime ISerializer<DateTime>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return System.DateTime.FromBinary(((ISerializer<long>)this).ReadFrom(stream, ref position));
    }

    #endregion
    #region ISerializer<TimeSpan> Members

    void ISerializer<TimeSpan>.WriteTo(TimeSpan value, IBufferWriter<byte> stream)
    {
        ((ISerializer<long>)this).WriteTo(value.Ticks, stream);
    }

    TimeSpan ISerializer<TimeSpan>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return new TimeSpan(((ISerializer<long>)this).ReadFrom(stream, ref position));
    }

    #endregion
    #region ISerializer<short> Members

    void ISerializer<short>.WriteTo(short value, IBufferWriter<byte> stream)
    {
        ((ISerializer<ushort>)this).WriteTo(unchecked((ushort)value), stream);
    }

    short ISerializer<short>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return unchecked((short)((ISerializer<ushort>)this).ReadFrom(stream, ref position));
    }

	#endregion
	#region ISerializer<ushort> Members

	void ISerializer<ushort>.WriteTo(ushort value, IBufferWriter<byte> stream)
    {
        BinaryPrimitives.WriteUInt16BigEndian(stream.GetSpan(2), value);
        stream.Advance(2);
	}

    ushort ISerializer<ushort>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
		Span<byte> b = stackalloc byte[2] ;

        if (!stream.TryRead(ref position, b))
            throw new InvalidDataException("Not enough data to read a UInt16");

		return BinaryPrimitives.ReadUInt16BigEndian(b);
    }

    #endregion
    #region ISerializer<int> Members

    void ISerializer<int>.WriteTo(int value, IBufferWriter<byte> stream)
    {
        ((ISerializer<uint>)this).WriteTo(unchecked((uint)value), stream);
    }

    int ISerializer<int>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return unchecked((int)((ISerializer<uint>)this).ReadFrom(stream, ref position));
    }

    #endregion
    #region ISerializer<uint> Members

    void ISerializer<uint>.WriteTo(uint value, IBufferWriter<byte> stream)
    {
        BinaryPrimitives.WriteUInt32BigEndian(stream.GetSpan(4), value);
        stream.Advance(4);
    }

    uint ISerializer<uint>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        Span<byte> buffer = stackalloc byte[4];

        if (!stream.TryRead(ref position, buffer))
            throw new InvalidDataException("Not enough data to read a UInt32");

        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

	#endregion
	#region ISerializer<long> Members

	void ISerializer<long>.WriteTo(long value, IBufferWriter<byte> stream)
    {
        ((ISerializer<ulong>)this).WriteTo(unchecked((ulong)value), stream);
    }

    long ISerializer<long>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return unchecked((long)((ISerializer<ulong>)this).ReadFrom(stream, ref position));
    }

    #endregion
    #region ISerializer<ulong> Members

    void ISerializer<ulong>.WriteTo(ulong value, IBufferWriter<byte> stream)
    {
        BinaryPrimitives.WriteUInt64BigEndian(stream.GetSpan(8), value);
        stream.Advance(8);
    }

    ulong ISerializer<ulong>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        Span<byte> buffer = stackalloc byte[8];

        if (!stream.TryRead(ref position, buffer))
            throw new InvalidDataException("Not enough data to read a UInt64");

        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }

    #endregion
    #region ISerializer<double> Members

    void ISerializer<double>.WriteTo(double value, IBufferWriter<byte> stream)
    {
        ((ISerializer<long>)this).WriteTo(BitConverter.DoubleToInt64Bits(value), stream);
    }

    double ISerializer<double>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return BitConverter.Int64BitsToDouble(((ISerializer<long>)this).ReadFrom(stream, ref position));
    }

    #endregion
    #region ISerializer<float> Members

    void ISerializer<float>.WriteTo(float value, IBufferWriter<byte> stream)
    {
        ((ISerializer<long>)this).WriteTo(BitConverter.DoubleToInt64Bits(value), stream);
    }

    float ISerializer<float>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return unchecked((float)BitConverter.Int64BitsToDouble(((ISerializer<long>)this).ReadFrom(stream, ref position)));
    }

    #endregion
    #region ISerializer<Guid> Members

    void ISerializer<Guid>.WriteTo(Guid value, IBufferWriter<byte> stream)
    {
        stream.Write(value.ToByteArray());
    }

    Guid ISerializer<Guid>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        Span<byte> buffer = stackalloc byte[16];

		if (!stream.TryRead(ref position, buffer))
            throw new InvalidDataException("Not enough data to read a Guid");

        return new Guid(buffer);
    }

    #endregion
    #region ISerializer<IntPtr> Members

    void ISerializer<IntPtr>.WriteTo(IntPtr value, IBufferWriter<byte> stream)
    {
        ((ISerializer<long>)this).WriteTo(value.ToInt64(), stream);
    }

    IntPtr ISerializer<IntPtr>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return new IntPtr(((ISerializer<long>)this).ReadFrom(stream, ref position));
    }

    #endregion
    #region ISerializer<UIntPtr> Members

    void ISerializer<UIntPtr>.WriteTo(UIntPtr value, IBufferWriter<byte> stream)
    {
        ((ISerializer<ulong>)this).WriteTo(value.ToUInt64(), stream);
    }

    UIntPtr ISerializer<UIntPtr>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    {
        return new UIntPtr(((ISerializer<ulong>)this).ReadFrom(stream, ref position));
    }

	#endregion

	void ISerializer<string>.WriteTo(string value, IBufferWriter<byte> stream)
	{
		if (value == null)
		{
			Int32.WriteTo(int.MinValue, stream);
		}
		else
		{
            var bytes = MemoryMarshal.Cast<char, byte>(value);
            var target = stream.GetSpan(bytes.Length + 4);
            BinaryPrimitives.WriteInt32BigEndian(target, value.Length);
            bytes.CopyTo(target.Slice(4));
            stream.Advance(bytes.Length + 4);
		}
	}

	string ISerializer<string>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
	{
		int sz = Int32.ReadFrom(stream, ref position);
		if (sz == 0) return string.Empty;
		if (sz == int.MinValue)
			return null;

        var res = string.Create(sz, (stream, position), (span, state) =>
        {
            var buffer = MemoryMarshal.Cast<char, byte>(span);
            if (!state.stream.TryRead(ref state.position, buffer))
                throw new InvalidDataException("Not enough data to read a String");
		});

        position = stream.GetPosition(sz*2, position);

		return res;
	}
}
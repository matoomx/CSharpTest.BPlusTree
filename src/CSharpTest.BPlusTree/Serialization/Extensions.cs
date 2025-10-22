using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.IO;

namespace CSharpTest.Collections.Generic;

internal static class Extensions
{
    public static void Write(this IBufferWriter<byte> target, byte data)
    {
        var d = target.GetSpan();
        d[0] = data;
        target.Advance(1);
    }

	public static byte ReadByte(this ReadOnlySequence<byte> source, ref SequencePosition position)
    {
        if (source.TryGet(ref position, out ReadOnlyMemory<byte> memory, false))
        {
            var span = memory.Span;
            if (span.Length > 0)
            {
                byte value = span[0];
                position = source.GetPosition(1, position);
                return value;
            }
        }
        throw new InvalidDataException("No more data available to read.");
	}

    public static bool TryRead(this ReadOnlySequence<byte> source, ref SequencePosition position, Span<byte> buffer)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length && source.TryGet(ref position, out ReadOnlyMemory<byte> memory, false))
        {
            var span = memory.Span;
            int toCopy = Math.Min(span.Length, buffer.Length - totalRead);
            span.Slice(0, toCopy).CopyTo(buffer.Slice(totalRead, toCopy));
            totalRead += toCopy;
            position = source.GetPosition(toCopy, position);
		}
        return totalRead == buffer.Length;
	}

    public static int Read(this SafeFileHandle handle, long fileOffset, Span<byte> target)
    {
		var leftToRead = target.Length;
		int read;
		int totalRead = 0;

		while (leftToRead > 0 && (read = RandomAccess.Read(handle, target.Slice(target.Length - leftToRead, leftToRead), fileOffset + totalRead)) > 0)
		{
			leftToRead -= read;
			totalRead += read;
		}

		if (leftToRead > 0)
			throw new IOException("Failed to read file");

        return totalRead;
	}

	public static int ReadAtLeast(this SafeFileHandle handle, long fileOffset, int minRead, Span<byte> target)
	{	
		int read;
		int totalRead = 0;

		while (minRead > 0 && (read = RandomAccess.Read(handle, target, fileOffset + totalRead)) > 0)
		{
            target.Slice(read);
			minRead -= read;
			totalRead += read;
		}

		if (minRead > 0)
			throw new IOException("Failed to read file");

		return totalRead;
	}
}

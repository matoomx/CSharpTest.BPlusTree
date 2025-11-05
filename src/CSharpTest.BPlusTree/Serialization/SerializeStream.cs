using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace CSharpTest.Collections.Generic;

public sealed class SerializeStream : IBufferWriter<byte> , IDisposable
{
	private const int DefaultBlockSize = 8192;
	private readonly List<byte[]> _rented = new(1);
	private readonly List<Memory<byte>> _old = new(0);
	private long _oldSize = 0;
	private Memory<byte> _current = Memory<byte>.Empty;
	private int _currentPos = 0;

	public long Position { get { return _oldSize + _currentPos; } }

	public Span<byte> GetSpan(int size)
    {
		size = Math.Max(size, 1);

		if (_current.Length < _currentPos + size)
			Grow(size);

		return _current.Span.Slice(_currentPos);
	}

	public Memory<byte> GetMemory(int size)
    {
		size = Math.Max(size, 1);

		if (_current.Length < _currentPos + size)
			Grow(size);

		return _current.Slice(_currentPos);
	}

    public void Advance(int count)
    {
		_currentPos += count;
	}

    public void Dispose()
    {
		foreach (var block in _rented)
			ArrayPool<byte>.Shared.Return(block);
	}

	public void Clear() 
	{
		if (_rented.Count > 1)
		{
			for (int i = _rented.Count - 1; i > 0; i--)
				ArrayPool<byte>.Shared.Return(_rented[i]);

			CollectionsMarshal.SetCount(_rented, 1);
			_current = _rented[0];
		}

		if (_old.Count > 0)
			_old.Clear();

		_oldSize = 0;
		_currentPos = 0;
	} 

	public bool IsSingleBlock => _old.Count == 0;

	public Memory<byte> GetFirstBlock()
	{
		if (_old.Count > 0)
			return _old[0];
		return _current.Slice(0, _currentPos);
	}




	public uint CalculateCrc32(int skipInitial = 0)
	{
		ArgumentOutOfRangeException.ThrowIfGreaterThan(skipInitial, DefaultBlockSize, nameof(skipInitial));

		if (IsSingleBlock)
			return System.IO.Hashing.Crc32.HashToUInt32(_current.Span.Slice(skipInitial, _currentPos - skipInitial));

		var crc = new System.IO.Hashing.Crc32();

		var firstBlock = true;

		foreach (var block in _old)
		{
			if (firstBlock)
			{
				crc.Append(block.Span.Slice(skipInitial));
				firstBlock = false;
			}
			else
				crc.Append(block.Span);
		}		

		crc.Append(_current.Span.Slice(0, _currentPos));
		return crc.GetCurrentHashAsUInt32();
	}


	public void WriteTo(SafeFileHandle fileHandle, long position, int minBlockSize = 0)
	{
		if (IsSingleBlock)
			RandomAccess.Write(fileHandle, _current.Span.Slice(0, Math.Max(minBlockSize, _currentPos)), position);
		else
			RandomAccess.Write(fileHandle, GetBlocks(), position);
	}

	private IReadOnlyList<ReadOnlyMemory<byte>> GetBlocks()
	{
		var list = new List<ReadOnlyMemory<byte>>(_old.Count + 1);
		
		foreach (var block in _old)
			list.Add(block);

		list.Add(_current.Slice(0, _currentPos));

		return list;
	}

	private void Grow(int needed)
	{
		if (_currentPos > 0)
		{
			_old.Add(_current.Slice(0, _currentPos));
			_oldSize += _currentPos;
			_currentPos = 0;
		}

		var newBlock = ArrayPool<byte>.Shared.Rent(Math.Max(DefaultBlockSize, needed));
		_rented.Add(newBlock);
		_current = newBlock;	
	}

}

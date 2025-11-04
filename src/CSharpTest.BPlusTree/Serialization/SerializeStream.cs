using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

public sealed class SerializeStream : IBufferWriter<byte> , IDisposable
{
	private const int DefaultBlockSize = 8192;
	private readonly List<byte[]> _rented = new(1);
	private readonly List<Memory<byte>> _old = new(1);
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

	public ReadOnlySequence<byte> GetReadOnlySequence()
	{
		if (_old.Count == 0)
			return new ReadOnlySequence<byte>(_current.Slice(0, _currentPos));

		var first = new BlockSegment(_old[0]);
		var last = first;

		for (int blockIdx = 1; last.RunningIndex + last.Memory.Length < Position; blockIdx++)
			last = last.Append(blockIdx == _old.Count ? _current.Slice(0, _currentPos) : _old[blockIdx]);

		return new ReadOnlySequence<byte>(first, 0, last, (int)(Position - last.RunningIndex));
	}

	public IReadOnlyList<ReadOnlyMemory<byte>> GetBlocks()
	{
		if (_old.Count == 0 )
			return new List<ReadOnlyMemory<byte>>(1) { _current.Slice(0, _currentPos) };

		var list = new List<ReadOnlyMemory<byte>>(_old.Count + 1);
		
		foreach(var block in _old)
			list.Add(block);

		list.Add(_current.Slice(0, _currentPos));
		return list;
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

	//public void WriteTo(Stream stream)
	//{
	//	foreach (var block in _old)
	//		stream.Write(block.Span);

	//	stream.Write(_current.Span.Slice(0, _currentPos));
	//}

	public void WriteTo(Span<byte> target)
	{
		foreach (var block in _old)
		{
			block.Span.CopyTo(target);
			target = target.Slice(block.Length);
		}

		_current.Span.Slice(0, _currentPos).CopyTo(target);
	}

	//public async ValueTask WriteToAsync(Stream stream)
	//   {
	//	foreach (var block in _old)
	//		await stream.WriteAsync(block).ConfigureAwait(false);

	//	await stream.WriteAsync(_current.Slice(0, _currentPos)).ConfigureAwait(false);
	//}

	//[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

	private sealed class BlockSegment : ReadOnlySequenceSegment<byte>
	{
		public BlockSegment(Memory<byte> memory) => this.Memory = memory;

		public BlockSegment Append(Memory<byte> memory)
		{
			var nextSegment = new BlockSegment(memory) { RunningIndex = this.RunningIndex + this.Memory.Length };
			this.Next = nextSegment;
			return nextSegment;
		}
	}
}

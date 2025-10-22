using System;
using System.IO;
using System.Collections.Generic;
using System.Buffers;
using System.Buffers.Binary;
using Microsoft.Win32.SafeHandles;

namespace CSharpTest.Collections.Generic;

public partial class OrderedEnumeration<T>
{
	private sealed partial class DeserializeStream : IDisposable
	{
		private const int HeaderSize = 4;
		private readonly SafeFileHandle _file;
		private long _filePosition;
		private BlockSequence _firstSequence;
		private BlockSequence _lastSequence;
		private ReadOnlySequence<byte> _data;
		private long _position;
		private long _end;

		public DeserializeStream(SafeFileHandle file)
		{
			_file = file;
		}

		public bool HasMoreData => _position != _end;

		public ReadOnlySequence<byte> Read()
		{
			if (_position == _end)
			{
				//The client has read all data, so we can release all blocks, but keep one block for the next read
				EnsureOneBlock();
				ReadData();

				if (_end == 0)
					return ReadOnlySequence<byte>.Empty;
			}
			else
				ReleaseDeserializedBlocks();

			while (_end < _position + HeaderSize)
				ReadMoreData();

			Span<byte> sizeBuffer = stackalloc byte[4];
			_data.Slice(_position, 4).CopyTo(sizeBuffer);
			int messageSize = BinaryPrimitives.ReadInt32BigEndian(sizeBuffer);
			_position += HeaderSize;

			if (messageSize < 0)
				throw new IOException($"Invalid MessageSize {messageSize}");

			while (_end < _position + messageSize)
				ReadMoreData();

			_position += messageSize;

			return _data.Slice(_position - messageSize, messageSize);
		}

		private void ReadData()
		{
			var block = _firstSequence.DataBlock;
			_end = RandomAccess.Read(_file, block.AsSpan(), _filePosition);
			_filePosition += _end;
		}

		private void ReadMoreData()
		{
			var free = (int)(_lastSequence.DataBlock.Length + _lastSequence.RunningIndex - _end);

			if (free == 0)
			{
				var block = ArrayPool<byte>.Shared.Rent(8192);
				free = block.Length;
				_lastSequence = _lastSequence.Append(block);
				_data = new ReadOnlySequence<byte>(_firstSequence, 0, _lastSequence, _lastSequence.DataBlock.Length);
			}

			var read = RandomAccess.Read(_file, _lastSequence.DataBlock.AsSpan((int)(_end - _lastSequence.RunningIndex), free), _filePosition);
			_filePosition += read;

			if (read == 0)
				throw new IOException($"Unexpected 0 read return, request count max size {free} position {_position} current endpos {_end} lastblockstart {_lastSequence.RunningIndex}");

			_end += read;
		}

		private void ReleaseDeserializedBlocks()
		{
			if ((_firstSequence?.Next?.RunningIndex ?? long.MaxValue) <= _position)
			{
				while ((_firstSequence.Next?.RunningIndex ?? long.MaxValue) <= _position)
				{
					ArrayPool<byte>.Shared.Return(_firstSequence.DataBlock);
					_firstSequence = (BlockSequence)_firstSequence.Next;
				}

				_position -= _firstSequence.RunningIndex;
				_end -= _firstSequence.RunningIndex;
				_firstSequence.ReIndex();

				_data = new ReadOnlySequence<byte>(_firstSequence, 0, _lastSequence, _lastSequence.DataBlock.Length);
			}
		}
		private void EnsureOneBlock()
		{
			_position = 0;
			_end = 0;

			if (_firstSequence != null)
			{
				if (_firstSequence.Next == null) //We already have exactly one block
					return;

				while (_firstSequence.Next != null) //Release all blocks until we have only one block left
				{
					ArrayPool<byte>.Shared.Return(_firstSequence.DataBlock);
					_firstSequence = (BlockSequence)_firstSequence.Next;
				}
			}
			else
				_firstSequence = new BlockSequence(ArrayPool<byte>.Shared.Rent(8192)); // Create the first sequence with a new block

			_firstSequence.ReIndex();
			_data = new ReadOnlySequence<byte>(_firstSequence, 0, _firstSequence, _firstSequence.DataBlock.Length);
			_lastSequence = _firstSequence;
		}

		private void ReleaseAllBlocks()
		{
			if (_firstSequence != null)
			{
				if (_firstSequence.Next == null)
					ArrayPool<byte>.Shared.Return(_firstSequence.DataBlock);
				else
					foreach (var block in _firstSequence.DataBlocks)
						ArrayPool<byte>.Shared.Return(block);

				_position = 0;
				_end = 0;
				_firstSequence = null;
				_lastSequence = null;
				_data = ReadOnlySequence<byte>.Empty;
			}
		}

		public void Dispose()
		{
			ReleaseAllBlocks();
		}
	}
}

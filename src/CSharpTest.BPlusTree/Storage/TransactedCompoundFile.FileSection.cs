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
using System.Linq;
using Microsoft.Win32.SafeHandles;

namespace CSharpTest.Collections.Generic;

public sealed partial class TransactedCompoundFile
{
	sealed class FileSection
    {
        const int _baseOffset = 0;
        readonly int BlockSize;
        readonly int BlocksPerSection;
        readonly long SectionSize;

        readonly int _sectionIndex;
        readonly long _sectionPosition;
        readonly byte[] _blockData;

        private bool _isDirty;

        private FileSection(int sectionIndex, int blockSize, bool create)
        {
            _sectionIndex = sectionIndex;
            BlockSize = blockSize;
            BlocksPerSection = BlockSize >> 2;
            SectionSize = BlockSize * BlocksPerSection;

            _sectionPosition = SectionSize * sectionIndex;
            _blockData = new byte[BlockSize];
            if (create)
            {
                MakeValid();
                _isDirty = true;
            }
        }

        public FileSection(int sectionIndex, int blockSize) : this(sectionIndex, blockSize, true)
        { }

        public static bool TryLoadSection(SafeFileHandle fileHandle, bool alt, int sectionIndex, int blockSize, out FileSection section)
        {
            section = new FileSection(sectionIndex, blockSize, false);
            
            var altBuffer = ArrayPool<byte>.Shared.Rent(blockSize);

            try
            {
                byte[] part1 = alt ? altBuffer : section._blockData;
				byte[] part2 = !alt ? altBuffer : section._blockData;

				fileHandle.Read(section._sectionPosition, part1.AsSpan(0, blockSize));
				fileHandle.Read(section._sectionPosition + (section.SectionSize - blockSize), part2.AsSpan(0, blockSize));

                section._isDirty = !part1.SequenceEqual(part2);
            }
            finally
            {
				ArrayPool<byte>.Shared.Return(altBuffer);
			}

            if (!section.CheckValid())
            {
                section = null;
                return false;
            }
            return true;
        }

        public void SetHandle(SafeFileHandle fileHandle, int index, uint blockId)
        {
            if (index <= 0 || index >= BlocksPerSection - 1)
                throw new InvalidDataException();
            
            WriteUInt32(index, blockId);
            _isDirty = true;

            if (fileHandle != null)
            {
                Commit(fileHandle, false);
                Commit(fileHandle, true);
            }
        }

        public void Commit(SafeFileHandle fileHandle, bool phase2)
        {
            if (!_isDirty)
                return;

            if (phase2 && ReadUInt32(0) != CalcCrc32())
                throw new InvalidDataException();
            else 
                MakeValid();

            long phaseShift = phase2 ? (SectionSize - BlockSize) : 0;
            RandomAccess.Write(fileHandle, _blockData, _sectionPosition + phaseShift);

            if (phase2)
                _isDirty = false;
        }

		public void Write(SafeFileHandle fileHandle, BlockRef block, SerializeStream source)
		{
            var span = source.GetFirstBlock().Span;
			//Write the header
			span[OffsetOfHeaderSize] = BlockHeaderSize;
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfLength), (uint)(int)source.Position - BlockHeaderSize);
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfCrc32), source.CalculateCrc32(BlockHeaderSize));
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfBlockCount), (uint)block.ActualBlocks);
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfBlockId), block.Identity);

            var position = _sectionPosition + (BlockSize * block.Offset);

			if (source.IsSingleBlock)
				RandomAccess.Write(fileHandle, source.GetFirstBlock().Span, position);
			else
				RandomAccess.Write(fileHandle, source.GetBlocks(), position);
		}

		public ReadData Read(SafeFileHandle fileHandle, BlockRef block, bool headerOnly)
        {
            bool retry;
            byte[] bytes;
            int readBytes, headerSize, length;
            do
            {
                retry = false;
                long position = _sectionPosition + (BlockSize * block.Offset);
                var byteArrayLength = headerOnly ? BlockHeaderSize : block.ActualBlocks * BlockSize;

                bytes = ArrayPool<byte>.Shared.Rent(byteArrayLength);
				//TODO consider reading in smaller chunks for large blocks
				readBytes = fileHandle.Read(position, bytes.AsSpan(0, byteArrayLength));
                
                if (readBytes < BlockHeaderSize)
                {
					ArrayPool<byte>.Shared.Return(bytes);
                    throw new InvalidDataException();
                }

                headerSize = bytes[OffsetOfHeaderSize];
                length = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfLength));
                block.ActualBlocks = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfBlockCount));
                uint blockId = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfBlockId));

                if(headerSize < BlockHeaderSize)
                {
					ArrayPool<byte>.Shared.Return(bytes);
					throw new InvalidDataException();
                }
                if (blockId != block.Identity)
                {
					ArrayPool<byte>.Shared.Return(bytes);
					throw new InvalidDataException();
                }
                if (block.Count < 16 && block.ActualBlocks != block.Count)
                {
					ArrayPool<byte>.Shared.Return(bytes);
					throw new InvalidDataException();
                }
                if ((block.Count == 16 && block.ActualBlocks < 16))
                {
					ArrayPool<byte>.Shared.Return(bytes);
					throw new InvalidDataException();
                }

                if (headerSize < BlockHeaderSize || blockId != block.Identity ||
                    ((block.Count < 16 && block.ActualBlocks != block.Count) ||
                     (block.Count == 16 && block.ActualBlocks < 16)))
                {
					ArrayPool<byte>.Shared.Return(bytes);
                    throw new InvalidDataException();
                }

                if (block.ActualBlocks != Math.Max(1, (length + headerSize + BlockSize - 1) / BlockSize))
                {
					ArrayPool<byte>.Shared.Return(bytes);
                    throw new InvalidDataException();
                }

                if (headerOnly)
                {
					ArrayPool<byte>.Shared.Return(bytes);
                    return ReadData.Empty;
                }

                if (readBytes < length + headerSize)
                {
                    retry = byteArrayLength != block.ActualBlocks * BlockSize;
                }
                if (retry)
                {
					ArrayPool<byte>.Shared.Return(bytes);
                }
            } while (retry);

            if (readBytes < length + headerSize)
            {
				ArrayPool<byte>.Shared.Return(bytes);
                throw new InvalidDataException();
            }

            var crc = System.IO.Hashing.Crc32.HashToUInt32(bytes.AsSpan(headerSize, length));

            if (crc != BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfCrc32)))
            {
				ArrayPool<byte>.Shared.Return(bytes);
                throw new InvalidDataException();
            }
            return new ReadData(bytes, headerSize, length);
        }

        public void GetFree(SafeFileHandle fileHandle, OrdinalList freeHandles, OrdinalList usedBlocks)
        {
            int baseHandle = unchecked(BlocksPerSection * _sectionIndex);
            //reserved: first and last block
            usedBlocks.Add(baseHandle);
            usedBlocks.Add(baseHandle + BlocksPerSection - 1);

            for (int handle = 1; handle < BlocksPerSection - 1; handle++)
            {
                uint data = ReadUInt32(handle);
                if (data == 0)
                    freeHandles.Add(baseHandle + handle);
                else
                {
                    BlockRef block = new BlockRef(data, BlockSize);
                    int blockId = (int)block.Identity & 0x0FFFFFFF;

                    if (block.Count == 16)
                    {
                        long position = (long)BlocksPerSection*BlockSize*block.Section;
                        position += BlockSize*block.Offset;
                        byte[] header = new byte[BlockHeaderSize];
                        if (BlockHeaderSize != fileHandle.Read(position, header))
                            throw new InvalidDataException();
                        block.ActualBlocks = (int)BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(OffsetOfBlockCount));
                    }

                    for (uint i = 0; i < block.ActualBlocks; i++)
                        usedBlocks.Add(blockId++);
                }
            }
        }

        public uint this[int index]
        {
            get 
            {
                if (index < 1 || index >= BlocksPerSection - 1)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return ReadUInt32(index);
            }
        }

        private uint ReadUInt32(int ordinal)
        {
            int offset = ordinal << 2;
            int start = _baseOffset + offset;
            lock (_blockData)
                return BinaryPrimitives.ReadUInt32LittleEndian(_blockData.AsSpan(start));
        }

        private void WriteUInt32(int ordinal, uint value)
        {
            int offset = ordinal << 2;
            int start = _baseOffset + offset;
            lock(_blockData)
                BinaryPrimitives.WriteUInt32LittleEndian(_blockData.AsSpan(start), value);
		}

        private void MakeValid()
        {
            uint crc = CalcCrc32();
            var span = _blockData.AsSpan();
            BinaryPrimitives.WriteUInt32LittleEndian(span, crc);
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(_blockData.Length - 4), crc);
        }

        private uint CalcCrc32()
        {
            return System.IO.Hashing.Crc32.HashToUInt32(_blockData.AsSpan(4, BlockSize - 8));
        }

        private bool CheckValid()
        {
			var span = _blockData.AsSpan();
			uint crc1 = BinaryPrimitives.ReadUInt32LittleEndian(span);
            uint crc2 = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(_blockData.Length - 4));
            return crc1 == crc2 && crc1 == CalcCrc32();
        }
    }
}

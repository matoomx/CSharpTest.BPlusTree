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
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using Microsoft.Win32.SafeHandles;

namespace CSharpTest.Collections.Generic;

public sealed partial class TransactedCompoundFile
{
	class FileSection
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

        public static bool TryLoadSection(SafeFileHandle handle, bool alt, int sectionIndex, int blockSize, out FileSection section)
        {
            section = new FileSection(sectionIndex, blockSize, false);
            byte[] part1 = alt ? new byte[blockSize] : section._blockData;

            RandomAccess.Read(handle, part1.AsSpan(0, blockSize), section._sectionPosition);

            byte[] part2 = !alt ? new byte[blockSize] : section._blockData;

			RandomAccess.Read(handle, part2.AsSpan(0, blockSize), section._sectionPosition + (section.SectionSize - blockSize));

			section._isDirty = !part1.SequenceEqual(part2);

            if (!section.CheckValid())
            {
                section = null;
                return false;
            }
            return true;
        }

        public void SetHandle(FPut fcommit, int index, uint blockId)
        {
            if (index <= 0 || index >= BlocksPerSection - 1)
                throw new InvalidDataException();
            WriteUInt32(index, blockId);
            _isDirty = true;

            if (fcommit != null)
            {
                Commit(fcommit, false);
                Commit(fcommit, true);
            }
        }

        public void Commit(FPut put, bool phase2)
        {
            if (!_isDirty)
                return;

            if (phase2 && ReadUInt32(0) != CalcCrc32())
                throw new InvalidDataException();
            else 
                MakeValid();

            long phaseShift = phase2 ? (SectionSize - BlockSize) : 0;
            put(_sectionPosition + phaseShift, _blockData);

            if (phase2)
                _isDirty = false;
        }

		public void Write(BlockRef block, FPutS fputs, SerializeStream source)
		{
            var span = source.GetFirstBlock().Span;
			//Write the header
			span[OffsetOfHeaderSize] = BlockHeaderSize;
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfLength), (uint)(int)source.Position - BlockHeaderSize);
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfCrc32), source.CalculateCrc32(BlockHeaderSize));
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfBlockCount), (uint)block.ActualBlocks);
			BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(OffsetOfBlockId), block.Identity);
			fputs(_sectionPosition + (BlockSize * block.Offset), source);
		}

		public ReadData Read(BlockRef block, bool headerOnly, FGet fget)
        {
            bool retry;
            byte[] bytes;
            int readBytes, headerSize, length;
            do
            {
                retry = false;
                long position = _sectionPosition + (BlockSize * block.Offset);
                var byteArrayLength = headerOnly ? BlockHeaderSize : block.ActualBlocks * BlockSize;

                bytes = _bytePool.Rent(byteArrayLength);

                readBytes = fget(position, bytes, byteArrayLength);
                if (readBytes < BlockHeaderSize)
                {
                    _bytePool.Return(bytes);
                    throw new InvalidDataException();
                }

                headerSize = bytes[OffsetOfHeaderSize];
                length = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfLength));
                block.ActualBlocks = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfBlockCount));
                uint blockId = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfBlockId));

                if(headerSize < BlockHeaderSize)
                {
                    throw new InvalidDataException();
                }
                if (blockId != block.Identity)
                {
                    throw new InvalidDataException();
                }
                if (block.Count < 16 && block.ActualBlocks != block.Count)
                {
                    throw new InvalidDataException();
                }
                if ((block.Count == 16 && block.ActualBlocks < 16))
                {
                    throw new InvalidDataException();
                }

                if (headerSize < BlockHeaderSize || blockId != block.Identity ||
                    ((block.Count < 16 && block.ActualBlocks != block.Count) ||
                     (block.Count == 16 && block.ActualBlocks < 16)))
                {
                    _bytePool.Return(bytes);
                    throw new InvalidDataException();
                }

                if (block.ActualBlocks != Math.Max(1, (length + headerSize + BlockSize - 1) / BlockSize))
                {
                    _bytePool.Return(bytes);
                    throw new InvalidDataException();
                }

                if (headerOnly)
                {
                    _bytePool.Return(bytes);
                    return ReadData.Empty;
                }

                if (readBytes < length + headerSize)
                {
                    retry = byteArrayLength != block.ActualBlocks * BlockSize;
                }
                if (retry)
                {
                    _bytePool.Return(bytes);
                }
            } while (retry);

            if (readBytes < length + headerSize)
            {
                _bytePool.Return(bytes);
                throw new InvalidDataException();
            }

            var crc = System.IO.Hashing.Crc32.HashToUInt32(bytes.AsSpan(headerSize, length));

            if (crc != BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(OffsetOfCrc32)))
            {
                _bytePool.Return(bytes);
                throw new InvalidDataException();
            }
            return new ReadData(bytes, _bytePool, headerSize, length);
        }

        public void GetFree(OrdinalList freeHandles, OrdinalList usedBlocks, FGet fget)
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
                        if (BlockHeaderSize != fget(position, header, header.Length))
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

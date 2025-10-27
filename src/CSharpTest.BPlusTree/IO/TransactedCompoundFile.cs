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
using System.IO;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Similar behavior to the FragmentedFile; however, a much improved implementation.  Allows for
/// file-level commit/rollback or write-by-write commits to disk.  By default provides data-protection
/// against process-crashes but not OS crashes.  Use FileOptions.WriteThrough to protect against
/// OS crashes and power outtages.
/// </summary>
public sealed partial class TransactedCompoundFile : IDisposable, ITransactable
{
    enum LoadFrom { FirstBlock, LastBlock, Either }
    delegate void FPut(long position, ReadOnlySpan<byte> data);
	delegate void FPutS(long position, SerializeStream data);
	delegate int FGet(long position, byte[] bytes, int length);

    /// <summary>
    /// Returns the first block that *would* be allocated by a call to Create() on an empty file.
    /// </summary>
    public static uint FirstIdentity { get { return 1; } }

    public const int BlockHeaderSize = 17; //Header Size + Length + CRC + Block Count + Block Id
    private const int OffsetOfHeaderSize = 0;
    private const int OffsetOfLength = 1;
    private const int OffsetOfCrc32 = 5;
    private const int OffsetOfBlockCount = 9;
    private const int OffsetOfBlockId = 13;

    readonly Options _options;
    readonly int BlockSize;
    readonly int BlocksPerSection;
    readonly long SectionSize;

    readonly Lock _sync;
    FileSection[] _sections;

    SafeFileHandle _fileHandle;
    FPut _fcommit;
    FPut _fput;
    FPutS _fputS;
	FGet _fget;

    int _firstFreeBlock, _prevFreeBlock, _prevFreeHandle;
    OrdinalList _freeHandles;
    OrdinalList _freeBlocks;
    OrdinalList _reservedBlocks;

    /// <summary>
    /// Creates or opens a TransactedCompoundFile using the filename specified.
    /// </summary>
    public TransactedCompoundFile(string filename) : this(new Options(filename) { CreateNew = !File.Exists(filename) })
    { }

    /// <summary>
    /// Creates or opens a TransactedCompoundFile using the filename specified.
    /// </summary>
    public TransactedCompoundFile(Options options)
    {
        _options = options.Clone();
        _sync = new Lock();
        BlockSize = _options.BlockSize;
        BlocksPerSection = BlockSize >> 2;
        SectionSize = BlockSize * BlocksPerSection;

        _freeHandles = [];
        _freeBlocks = [];
        _reservedBlocks = [];

		_fileHandle = File.OpenHandle(
            options.FilePath, 
            _options.CreateNew ? FileMode.Create : FileMode.Open,
			_options.ReadOnly ? FileAccess.Read : FileAccess.ReadWrite,
			_options.ReadOnly ? FileShare.ReadWrite : FileShare.Read,
		   _options.FileOptions);

		_fcommit = _fput = WriteSpan;
		_fputS = WriteSegments;
		_fget = ReadBytes;

        try
        {
            LoadSections(_fileHandle);
            if (_sections.Length == 0)
                AddSection();
        }
        catch
        {
			_fileHandle.Dispose();
            throw;
        }

        if (!_options.CommitOnWrite)
        {
            _fcommit = null;
        }
	}

	private void WriteSpan(long position, ReadOnlySpan<byte> data)
    {
		RandomAccess.Write(_fileHandle, data, position);
    }

	private void WriteSegments(long position, SerializeStream data)
	{
        if (data.IsSingleBlock)
            RandomAccess.Write(_fileHandle, data.GetFirstBlock().Span, position);
        else
			RandomAccess.Write(_fileHandle, data.GetBlocks(), position);
	}

	/// <summary>
	/// Closes all streams and clears all in-memory data.
	/// </summary>
	public void Dispose()
    {
        lock (_sync)
        {
            try
            {
                if (_fileHandle != null && _options.CommitOnDispose)
                    Commit();
            }
            finally
            {
                _fileHandle?.Dispose();
				_fileHandle = null;
                _freeHandles.Clear();
                _freeBlocks.Clear();
                _reservedBlocks.Clear();
                _sections = [];
            }
        }
    }

    private static void FlushStream(Stream stream)
    {
		if (stream is not FileStream fs)
			stream.Flush();
		else
			fs.Flush(true);
	}

    /// <summary>
    /// Flushes any pending writes to the disk and returns.
    /// </summary>
    void Flush()
    {
		ObjectDisposedException.ThrowIf(_fileHandle == null, this);

        lock (_sync)
			RandomAccess.FlushToDisk(_fileHandle);
    }
    /// <summary>
    /// For file-level transactions, performs a two-stage commit of all changed handles.
    /// </summary>
    public void Commit()
    {
        Commit(null, 0);
    }
    /// <summary>
    /// For file-level transactions, performs a two-stage commit of all changed handles.
    /// After the first stage has completed, the stageCommit() delegate is invoked.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public void Commit<T>(Action<T> stageCommit, T value)
    {
        if (_options.CommitOnWrite)
        {
            Flush();
            return;
        }

        lock(_sync)
        {
			ObjectDisposedException.ThrowIf(_fileHandle == null, this);

			//Phase 1 - commit block 0 for each section
			foreach (var section in _sections)
                section.Commit(_fput, false);
            
            Flush();

            try 
            {
				stageCommit?.Invoke(value);
			}
            finally
            {
                //Phase 2 - commit block max for each section and set clean
                foreach (var section in _sections)
                    section.Commit(_fput, true);
                
                Flush();

                foreach (int ifree in _reservedBlocks)
                {
                    _firstFreeBlock = Math.Min(_firstFreeBlock, ifree);
                    break;
                }
                _reservedBlocks = _freeBlocks.Invert((_sections.Length*BlocksPerSection) - 1);
            }
        }
    }
    /// <summary>
    /// For file-level transactions, Reloads the file from it's original (or last committed) state.
    /// </summary>
    /// <exception cref="InvalidOperationException">When CommitOnWrite is true, there is no going back.</exception>
    public void Rollback()
    {
        if (_options.CommitOnWrite)
            throw new InvalidOperationException();

        lock (_sync)
        {
				ObjectDisposedException.ThrowIf(_fileHandle == null, this);

				LoadSections(_fileHandle);
        }
    }

    private void LoadSections(SafeFileHandle handle)
    {
        switch(_options.LoadingRule)
        {
            case LoadingRule.Primary:
                if (!LoadSections(handle, LoadFrom.FirstBlock))
                    throw new InvalidDataException();
                break;
            case LoadingRule.Secondary:
                if (!LoadSections(handle, LoadFrom.LastBlock))
                    throw new InvalidDataException();
                break;
            case LoadingRule.Default:
            default:
                if (!LoadSections(handle, LoadFrom.FirstBlock))
                    if (!LoadSections(handle, LoadFrom.LastBlock))
                        if (!LoadSections(handle, LoadFrom.Either))
                            throw new InvalidDataException();
                break;
        }
    }

    private bool LoadSections(SafeFileHandle handle, LoadFrom from)
    {
        long fsize = RandomAccess.GetLength(handle);
        long hsize = fsize / SectionSize;
        var sections = new FileSection[hsize];

        for (int i = 0; i < sections.Length; i++)
        {
            if (from == LoadFrom.Either && FileSection.TryLoadSection(handle, false, i, BlockSize, out sections[i]))
                continue;

            if (!FileSection.TryLoadSection(handle, from != LoadFrom.FirstBlock, i, BlockSize, out sections[i]))
                return false;
        }

        int lastIndex = (int)(hsize * BlocksPerSection) - 1;

		OrdinalList freeHandles = new OrdinalList
		{
			Ceiling = lastIndex
		};

		OrdinalList usedBlocks = new OrdinalList
		{
			Ceiling = lastIndex
		};

		foreach (var section in sections)
            section.GetFree(freeHandles, usedBlocks, _fget);

        _sections = sections;
        _freeHandles = freeHandles;
        _freeBlocks = usedBlocks.Invert(lastIndex);
        if (!_options.CommitOnWrite)
        {
            _reservedBlocks = usedBlocks;
        }
        _firstFreeBlock = _prevFreeBlock = _prevFreeHandle = 0;
        return true;
    }

    private int AddSection()
    {
        FileSection n = new FileSection(_sections.Length, BlockSize);
        lock (_sync)
        {
            n.Commit(_fput, false);
            n.Commit(_fput, true);
        }

        FileSection[] grow = new FileSection[_sections.Length + 1];
        _sections.CopyTo(grow, 0);
        grow[_sections.Length] = n;

        OrdinalList freeblocks = _freeBlocks.Clone();
        freeblocks.Ceiling = (grow.Length * BlocksPerSection) - 1;

        OrdinalList freehandles = _freeHandles.Clone();
        freehandles.Ceiling = (grow.Length * BlocksPerSection) - 1;
        // First and last handles/blocks are reserved by the section
        int lastFree = grow.Length * BlocksPerSection - 1;
        int firstFree = lastFree - BlocksPerSection + 2;
        for (int i = firstFree; i < lastFree; i++)
        {
            freehandles.Add(i);
            freeblocks.Add(i);
        }

        _sections = grow;
        _freeHandles = freehandles;
        _freeBlocks = freeblocks;
        return firstFree;
    }

    private uint TakeBlocks(int blocksNeeded)
    {
        lock (_sync)
        {
            bool rescan = false;
            bool resized = false;
            int startingFrom = _prevFreeBlock;
            int endingBefore = int.MaxValue;
            while (true)
            {
                int found = 0;
                int last = int.MinValue;
                int first = int.MaxValue;
                foreach (int free in _freeBlocks.EnumerateRange(startingFrom, endingBefore))
                {
                    if (_reservedBlocks.Contains(free))
                        continue;

                    if (found == 0)
                    {
                        _prevFreeBlock = free;
                        if (!resized && rescan)
                            _firstFreeBlock = free;
                    }

                    first = Math.Min(first, free);
                    found = (last + 1 != free) ? 1 : found + 1;
                    last = free;
                    if (found == blocksNeeded)
                    {
                        int start = free - (blocksNeeded - 1);
                        for (int i = start; i <= free; i++)
                            _freeBlocks.Remove(i);

                        uint blockId = (uint) start;
                        blockId |= ((uint) Math.Min(16, blocksNeeded) - 1 << 28) & 0xF0000000u;
                        return blockId;
                    }
                }
                if (resized)
                    throw new ArgumentOutOfRangeException(nameof(blocksNeeded));

                if (!rescan && _firstFreeBlock < startingFrom)
                {
                    rescan = true;
                    endingBefore = startingFrom + blocksNeeded - 1;
                    startingFrom = _firstFreeBlock;
                }
                else
                {
                    resized = true;
                    startingFrom = AddSection();
                    endingBefore = int.MaxValue;
                }
            }
        }
    }


    private void FreeBlocks(BlockRef block)
    {
        int free = (block.Section * BlocksPerSection) + block.Offset;
        if (free > 0)
        {
            _firstFreeBlock = Math.Min(_firstFreeBlock, free);

            if(block.ActualBlocks == 16)
            {
                using (_sections[block.Section].Read(block, true, _fget))
                { }
                if (((block.Count < 16 && block.ActualBlocks != block.Count) ||
                     (block.Count == 16 && block.ActualBlocks < 16)))
                    throw new InvalidDataException();
            }

            for (int i = 0; i < block.ActualBlocks; i++)
                _freeBlocks.Add(free + i);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="position"></param>
    /// <param name="bytes">Buffer</param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    private int ReadBytes(long position, byte[] bytes, int length)
    {
        return _fileHandle.Read(position, bytes.AsSpan(0, length));
    }

	/// <summary>
	/// Allocates a handle for data, you MUST call Write to commit the handle, otherwise the handle
	/// may be reallocated after closing and re-opening this file.  If you do not intend to commit
	/// the handle by writing to it, you should still call Delete() so that it may be reused.
	/// </summary>
	public uint Create()
    {
        uint handle = 0;
        lock (_sync)
        {
            while (handle == 0)
            {
                foreach (int i in _freeHandles.EnumerateFrom(_prevFreeHandle))
                {
                    _freeHandles.Remove(i);
                    _prevFreeHandle = i + 1;
                    handle = (uint)i;
                    break;
                }
                if (handle == 0)
                    AddSection();
            }
        }

        HandleRef href = new HandleRef(handle, BlockSize);
        uint blockId = _sections[href.Section][href.Offset];
        if (blockId != 0)
            throw new InvalidDataException();

        return handle;
    }

	/// <summary>
	/// Writes the bytes provided to the handle that was previously obtained by a call to Create().
	/// The length must not be more than ((16*BlockSize)-32) bytes in length.  The exact header size
	/// (32 bytes) may change without notice in a future release.
	/// </summary>
	public void Write(uint handle, SerializeStream data)
	{
		var href = new HandleRef(handle, BlockSize);
		if (handle == 0 || href.Section >= _sections.Length || _freeHandles.Contains((int)handle))
			throw new ArgumentOutOfRangeException(nameof(handle));

		uint oldblockId = _sections[href.Section][href.Offset];

		int blocksNeeded = Math.Max(1, ((int)data.Position + BlockSize - 1) / BlockSize);
		if (blocksNeeded > BlocksPerSection - 2)
			throw new ArgumentOutOfRangeException(nameof(data));

		uint blockId = TakeBlocks(blocksNeeded);
		var block = new BlockRef(blockId, BlockSize, blocksNeeded);

		lock (_sync)
		{
			_sections[block.Section].Write(block, _fputS, data);
			_sections[href.Section].SetHandle(_fcommit, href.Offset, blockId);
			if (oldblockId != 0)
				FreeBlocks(new BlockRef(oldblockId, BlockSize));
		}
	}


	/// <summary>
	/// Reads all bytes from the from the handle specified
	/// </summary>
	public ReadData Read(uint handle)
    {
        var href = new HandleRef(handle, BlockSize);
        
        if (handle == 0 || href.Section >= _sections.Length || _freeHandles.Contains((int)handle))
            throw new ArgumentOutOfRangeException(nameof(handle));

        uint blockId = _sections[href.Section][href.Offset];
        
        if (blockId == 0) 
            return ReadData.Empty;

        if (_freeBlocks.Contains((int)blockId & 0x0FFFFFFF))
            throw new InvalidDataException();

        var block = new BlockRef(blockId, BlockSize);
        return _sections[block.Section].Read(block, false, _fget);
    }
    /// <summary>
    /// Deletes the handle and frees the associated block space for reuse.
    /// </summary>
    public void Delete(uint handle)
    {
        HandleRef href = new HandleRef(handle, BlockSize);
        if (handle == 0 || href.Section >= _sections.Length || _freeHandles.Contains((int)handle))
            throw new ArgumentOutOfRangeException(nameof(handle));

        uint oldblockId = _sections[href.Section][href.Offset];
        lock (_sync)
        {
            _sections[href.Section].SetHandle(_fcommit, href.Offset, 0);

            if (oldblockId != 0)
                FreeBlocks(new BlockRef(oldblockId, BlockSize));
            _freeHandles.Add((int)handle);
            _prevFreeHandle = Math.Min(_prevFreeHandle, (int)handle);
        }
    }
    /// <summary>
    /// Immediatly truncates the file to zero-length and re-initializes an empty file
    /// </summary>
    public void Clear()
    {
        lock (_sync)
        {
            RandomAccess.SetLength(_fileHandle, 0);
            _freeBlocks.Clear();
            _freeHandles.Clear();
            _reservedBlocks.Clear();
            _firstFreeBlock = _prevFreeBlock = _prevFreeHandle = 0;

            _sections = [];
            AddSection();
        }
    }
}

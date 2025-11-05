#region Copyright 2012-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// The default transaction log for a BPlusTree instance to provide backup+log recovery
/// </summary>
public sealed partial class TransactionLog<TKey, TValue> : ITransactionLog<TKey, TValue>
{
    private const int StateOpen = 1, StateCommitted = 2, StateRolledback = 3;
    private readonly Lock _logSync;
    private readonly TransactionLogOptions<TKey, TValue> _options;
    private long _transactionId;
    private long _fLength;
    private SafeFileHandle _logfile;

    /// <summary>
    /// Creates an instance of a transaction log
    /// </summary>
    public TransactionLog(TransactionLogOptions<TKey, TValue> options)
    {
        _options = options.Clone();
        _logSync = new Lock();
        _transactionId = 1;
        _logfile = null;
        try
        {
            _fLength = File.Exists(_options.FileName) ? new FileInfo(_options.FileName).Length : 0;
        }
        catch (FileNotFoundException)
        {
            _fLength = 0;
        }
    }

    /// <summary>
    /// Returns the file name of the current transaction log file
    /// </summary>
    public string FileName { get { return _options.FileName; } }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    void IDisposable.Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
	}
    /// <summary>
    /// Flushes any pending writes and closes the writer.
    /// </summary>
    public void Close()
    {
        lock(_logSync)
        {
            if (_logfile != null)
            {
                RandomAccess.FlushToDisk(_logfile);
                _logfile.Dispose();
                _logfile = null;
            }

            if (Size == 0)
                File.Delete(_options.FileName);
        }
    }

    /// <summary>
    /// Returns the current size of the log file in bytes
    /// </summary>
    public long Size
    { 
        get
        {
            return _logfile != null ? _fLength : (File.Exists(_options.FileName) ? new FileInfo(_options.FileName).Length : 0); 
        }
    }
    
    /// <summary>
    /// Replay the entire log file to the provided dictionary interface
    /// </summary>
    public void ReplayLog(IDictionary<TKey, TValue> target)
    {
        long position = 0L;
        ReplayLog(target, ref position);
    }
    /// <summary>
    /// Replay the log file from the position provided and output the new log position
    /// </summary>
    public void ReplayLog(IDictionary<TKey, TValue> target, ref long position)
    {
		//in iterator methods we cant pass ref parameters, so use array
		long[] refposition = [position];
		try
        {
            lock (_logSync)
            {
                foreach (LogEntry entry in EnumerateLog(refposition))
                {
                    if (entry.OpCode == OperationCode.Remove)
                        target.Remove(entry.Key);
                    else
                        target[entry.Key] = entry.Value;
                }
            }
        }
        finally
        {
            position = refposition[0];
        }
    }

    /// <summary>
    /// Merges the contents of the log with an existing ordered key/value pair collection.
    /// </summary>
    public IEnumerable<KeyValuePair<TKey, TValue>> MergeLog(IComparer<TKey> keyComparer, IEnumerable<KeyValuePair<TKey, TValue>> existing)
    {
        _logSync.Enter();
        try
        {
            // Order the log entries by key
            var comparer = new LogEntryComparer(keyComparer);
            var orderedLog = new OrderedEnumeration<LogEntry>(
                comparer,
                EnumerateLog(new long[1]),
                new LogEntrySerializer(_options.KeySerializer, _options.ValueSerializer)
                );

            // Merge the existing data with the ordered log, using last value
            var all = OrderedEnumeration<LogEntry>.Merge(comparer, DuplicateHandling.LastValueWins, LogEntry.FromKeyValuePairs(existing), orderedLog);

            // Returns all key/value pairs that are not a remove operation
            foreach (LogEntry le in all)
            {
                if (le.OpCode != OperationCode.Remove)
                    yield return new KeyValuePair<TKey, TValue>(le.Key, le.Value);
            }
        }
        finally 
        { 
            _logSync.Exit(); 
        }
    }

    /// <summary>
    /// Replay the log file from the position provided and output the new log position
    /// </summary>
    IEnumerable<LogEntry> EnumerateLog(long[] position)
    {
        long pos = 0;
        long length;

        if (!File.Exists(_options.FileName))
        {
            position[0] = 0;
            yield break;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
			using var io = new FileStream(_options.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0x10000, FileOptions.SequentialScan);
			bool valid = true;
			const int minSize = 16;
			int size, temp, nbytes, szcontent;
			short opCount;
			var entry = new LogEntry();

			length = io.Length;
			if (position[0] < 0 || position[0] > length)
			{
				position[0] = length;
				yield break;
			}

			bool fixedOffset = position[0] > 0;
			io.Position = position[0];

			while (valid && (pos = position[0] = io.Position) + minSize < length)
			{
				try
				{
					size = ReadIntFromStream(io);
					size = ((byte)(size >> 24) == 0xbb) ? size & 0x00FFFFFF : -1;
					if (size < minSize || pos + size + 4 > length)
					{
						if (fixedOffset)
							yield break;
						break;
					}
					fixedOffset = false;

					if (size > buffer.Length)
					{
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = null;
						buffer = ArrayPool<byte>.Shared.Rent(size + 8192);
					}

					szcontent = size - 8;

					nbytes = 0;
					while (nbytes < szcontent && (temp = io.Read(buffer, nbytes, szcontent - nbytes)) != 0)
						nbytes += temp;

					if (nbytes != szcontent)
						break;

                    var span = buffer.AsSpan(0, nbytes);
					var crc = System.IO.Hashing.Crc32.HashToUInt32(span);
					if (crc != ReadUintFromStream(io))
						break;

					temp = ReadIntFromStream(io);
					if ((byte)(temp >> 24) != 0xee || (temp & 0x00FFFFFF) != size)
						break;

					entry.TransactionId = BinaryPrimitives.ReadInt32LittleEndian(span);
					_transactionId = Math.Max(_transactionId, entry.TransactionId + 1);

					opCount = BinaryPrimitives.ReadInt16LittleEndian(span.Slice(4));
					if (opCount <= 0 || opCount >= short.MaxValue)
						break;
				}
				catch (InvalidDataException)
				{
					break;
				}

                var data = new ReadOnlySequence<byte>(buffer.AsMemory(6, nbytes -6));
				var seqPos = data.Start;
                var end = data.End;

				while (opCount-- > 0)
				{
					entry.OpCode = (OperationCode)PrimitiveSerializer.Int16.ReadFrom(data, ref seqPos);

					if (entry.OpCode != OperationCode.Add && entry.OpCode != OperationCode.Update && entry.OpCode != OperationCode.Remove)
					{
						valid = false;
						break;
					}

					try
					{
						entry.Key = _options.KeySerializer.ReadFrom(data, ref seqPos);
						entry.Value = (entry.OpCode == OperationCode.Remove)
							? default
							: _options.ValueSerializer.ReadFrom(data, ref seqPos);
					}
					catch
					{
						valid = false;
						break;
					}
					if ((seqPos.Equals(end)) != (opCount == 0))
					{
						valid = false;
						break;
					}

					yield return entry;
				}
			}
		}
        finally
        {
			if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
		}

        if (!_options.ReadOnly && pos < length)
            TruncateLog(pos);
    }

    /// <summary>
    /// Truncate the log and remove all existing entries
    /// </summary>
    public void TruncateLog()
    {
        TruncateLog(0);
    }

    void TruncateLog(long position)
    {
        lock (_logSync)
        {
            Close();
			using var io = File.OpenHandle(_options.FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
			RandomAccess.SetLength(io, position);
			_fLength = position;
		}
    }

    /// <summary>
    /// Notifies the log that a transaction is begining and create a token for this
    /// transaction scope.
    /// </summary>
    public TransactionToken BeginTransaction()
    {
        return new TransactionToken
        {
            State = StateOpen,
            Handle = Interlocked.Increment(ref _transactionId),
        };
    }

    /// <summary> The provided key/value pair was added in the provided transaction </summary>
    public void AddValue(ref TransactionToken token, TKey key, TValue value)
    {
        Write(ref token, OperationCode.Add, key, value);
    }

    /// <summary> The provided key/value pair was updated in the provided transaction </summary>
    public void UpdateValue(ref TransactionToken token, TKey key, TValue value)
    {
        Write(ref token, OperationCode.Update, key, value);
    }

    /// <summary> The provided key/value pair was removed in the provided transaction </summary>
    public void RemoveValue(ref TransactionToken token, TKey key)
    {
        Write(ref token, OperationCode.Remove, key, default);
    }

    private void Write(ref TransactionToken token, OperationCode operation, TKey key, TValue value)
    {
        Check.Assert(token.State == StateOpen, "Transaction not open");
        var buffer = token.Stream;
        if (buffer == null)
        {
            token.Stream = buffer = new SerializeStream();
            buffer.GetSpan(4); //reserve space for header
            buffer.Advance(4);
            PrimitiveSerializer.Int32.WriteTo(unchecked((int)token.Handle), buffer);
            PrimitiveSerializer.Int16.WriteTo(0, buffer);
        }

		PrimitiveSerializer.Int16.WriteTo((short)operation, buffer);
        _options.KeySerializer.WriteTo(key, buffer);
        
        if (operation != OperationCode.Remove)
            _options.ValueSerializer.WriteTo(value, buffer);

		token.OperationCount++;
    }

    /// <summary>
    /// Commits the provided transaction
    /// </summary>
    public void CommitTransaction(ref TransactionToken token)
    {
        Check.Assert(token.State == StateOpen, "Transaction not open");
        token.State = StateCommitted;

        var buffer = token.Stream;
        if (buffer == null || buffer.Position == 0)
            return; // nothing to commit

        var suffix = buffer.GetSpan(8); //Get 8 bytes for Lenght and Crc at end of strem
		var first = buffer.GetFirstBlock().Span;

        BinaryPrimitives.WriteInt16LittleEndian(first.Slice(8, 2), token.OperationCount); 
		BinaryPrimitives.WriteUInt32LittleEndian(suffix, buffer.CalculateCrc32(4));
        buffer.Advance(4);
		int len = (int)buffer.Position;
		BinaryPrimitives.WriteInt32LittleEndian(suffix.Slice(4, 4), (0xee << 24) + len);
		buffer.Advance(4);
		BinaryPrimitives.WriteInt32LittleEndian(first.Slice(0, 4), (0xbb << 24) + len);

		if (buffer.IsSingleBlock)
			WriteBytes(buffer.GetFirstBlock().Span);
        else
			WriteBytes(buffer.GetBlocks(), buffer.Position);
	}

    private void WriteBytes(ReadOnlySpan<byte> bytes)
    {
        if (_options.ReadOnly) 
            return;
        
        lock (_logSync)
        {
            _logfile ??= File.OpenHandle(_options.FileName, FileMode.Append, FileAccess.Write, FileShare.Read, _options.FileOptions);           
            RandomAccess.Write(_logfile, bytes, _fLength);
            _fLength += bytes.Length;
        }
    }

	private void WriteBytes(IReadOnlyList<ReadOnlyMemory<byte>> bytes, long size)
	{
		if (_options.ReadOnly) 
            return;
		
        lock (_logSync)
		{
			_logfile ??= File.OpenHandle(_options.FileName, FileMode.Append, FileAccess.Write, FileShare.Read, _options.FileOptions);
			RandomAccess.Write(_logfile, bytes, _fLength);
			_fLength += size;
		}
	}

	/// <summary>
	/// Abandons the provided transaction
	/// </summary>
	public void RollbackTransaction(ref TransactionToken token)
    {
        if (token.State == StateRolledback)
            return;

        Check.Assert(token.State == StateOpen, "Transaction not open");
        token.State = StateRolledback;
        var buffer = token.Stream;
        buffer?.Dispose();
        token.Stream = null;
        token.Handle = 0;
        token.OperationCount = 0;
	}

    private static uint ReadUintFromStream(FileStream stream)
    {
		Span<byte> buffer = stackalloc byte[4];
		stream.ReadExactly(buffer);
		return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
	}

	private static int ReadIntFromStream(FileStream stream)
	{
		Span<byte> buffer = stackalloc byte[4];
		stream.ReadExactly(buffer);
		return BinaryPrimitives.ReadInt32LittleEndian(buffer);
	}
}

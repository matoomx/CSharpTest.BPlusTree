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

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Provides a file-based storage for the BPlusTree dictionary
/// </summary>
sealed partial class BTreeFileStore : INodeStorage, ITransactable
{
    private readonly TransactedCompoundFile _file;
    private readonly FileId _rootId;
    private readonly bool _readonly;

	/// <summary>
	/// Opens an existing BPlusTree file at the path specified, for a new file use CreateNew()
	/// </summary>
	public BTreeFileStore(TransactedCompoundFile.Options options)
    {
        _file = new TransactedCompoundFile(options);
        _rootId = new FileId(TransactedCompoundFile.FirstIdentity);
		_readonly = options.ReadOnly;

        if (options.CreateNew)
        {
			if (_readonly)
				throw new InvalidOperationException("Read only");

			CreateRoot(_file); 
        }
    }

    /// <summary>
    /// Closes the file in it's current state.
    /// </summary>
    public void Dispose()
    {
        _file.Dispose();
    }

    public void Commit()
    {
        _file.Commit();
    }

    public void Rollback()
    {
        _file.Rollback();
    }

    private static void CreateRoot(TransactedCompoundFile file)
    {
        uint rootId = file.Create();

        if (rootId != TransactedCompoundFile.FirstIdentity)
            throw new InvalidNodeHandleException();

        using var ms = new SerializeStream();
        ms.GetSpan(TransactedCompoundFile.BlockHeaderSize); //Make room for the block header
		ms.Advance(TransactedCompoundFile.BlockHeaderSize);        
        file.Write(rootId, ms);
		file.Commit();
    }

    public void Reset()
    {
        _file.Clear();
        CreateRoot(_file);
    }

    public IStorageHandle OpenRoot(out bool isNew)
    {
        using (var s = _file.Read(_rootId.Id))
            isNew = s.Data.IsEmpty;

        return _rootId;
    }

    public bool TryGetNode<TNode>(IStorageHandle handleIn, out TNode node, ISerializer<TNode> serializer)
    {
        if (handleIn is not FileId handle)
            throw new InvalidNodeHandleException();

		using var s = _file.Read(handle.Id);
        var pos = s.Data.Start;
		node = serializer.ReadFrom(s.Data, ref pos);
		return true;
	}

    public IStorageHandle Create()
    {
        return new FileId(_file.Create());
	}

    public void Destroy(IStorageHandle handleIn)
    {
       if (handleIn is not FileId handle)
            throw new InvalidNodeHandleException();

		_file.Delete(handle.Id);
    }

    public void Update<T>(IStorageHandle handleIn, ISerializer<T> serializer, T node)
    {
		if (handleIn is not FileId handle)
            throw new InvalidNodeHandleException();

        if (_readonly)
			throw new InvalidOperationException("Read only");

		using var ms = new SerializeStream();
        ms.GetSpan(TransactedCompoundFile.BlockHeaderSize); //Make room for the block header
		ms.Advance(TransactedCompoundFile.BlockHeaderSize);
		serializer.WriteTo(node, ms);
		_file.Write(handle.Id, ms);
	}

    void ISerializer<IStorageHandle>.WriteTo(IStorageHandle handleIn, IBufferWriter<byte> stream)
    {
		if (handleIn is not FileId handle)
			throw new InvalidNodeHandleException();

		PrimitiveSerializer.UInt32.WriteTo(handle.Id, stream);
        PrimitiveSerializer.UInt32.WriteTo(handle.Unique, stream);
    }

    IStorageHandle ISerializer<IStorageHandle>.ReadFrom(ReadOnlySequence<byte> stream , ref SequencePosition position)
    {
        return new FileId(PrimitiveSerializer.UInt32.ReadFrom(stream, ref position), PrimitiveSerializer.UInt32.ReadFrom(stream, ref position)); 
    }
}

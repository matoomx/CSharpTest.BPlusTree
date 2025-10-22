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
/// Provides an in-memory implementation of the storage services for BPlusTree, useful when testing :)
/// </summary>
partial class BTreeMemoryStore : INodeStorage
{
    readonly ISerializer<string> _stringSerializer;
    MyStorageHandle _root;

    /// <summary> Default in-memory storage </summary>
    public BTreeMemoryStore()
    {
        _stringSerializer = PrimitiveSerializer.Instance;
    }

    public void Dispose()
    {
        _root = null;
    }
    
    public IStorageHandle OpenRoot(out bool isNew)
    {
        isNew = _root == null;
        _root ??= new MyStorageHandle("ROOT");
        return _root;
    }

    public void Reset()
    {
        _root = null;
    }

    public bool TryGetNode<TNode>(IStorageHandle handleIn, out TNode node, ISerializer<TNode> serializer)
    {
        if (handleIn is not MyStorageHandle handle)
            throw new InvalidNodeHandleException();

		if (handle.Node != null)
        {
            node = (TNode)handle.Node;
            return true;
        }
        node = default;
        return false;
    }

    [Obsolete("Not supported", true)]
    void ISerializer<IStorageHandle>.WriteTo(IStorageHandle value, IBufferWriter<byte> stream)
    { throw new NotSupportedException(); }

    [Obsolete("Not supported", true)]
    IStorageHandle ISerializer<IStorageHandle>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    { throw new NotSupportedException(); }

    public IStorageHandle Create()
    {
        return new MyStorageHandle(); 
    }

    public void Destroy(IStorageHandle handleIn)
    {
		if (handleIn is not MyStorageHandle handle)
			throw new InvalidNodeHandleException();

		handle.Clear();
    }

    public void Update<T>(IStorageHandle handleIn, ISerializer<T> serializer, T node)
    {
		if (handleIn is not MyStorageHandle handle)
			throw new InvalidNodeHandleException();
        
        handle.Node = node;
    }
}
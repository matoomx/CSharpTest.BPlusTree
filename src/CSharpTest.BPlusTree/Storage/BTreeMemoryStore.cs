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
using System.Collections.Concurrent;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Provides an in-memory implementation of the storage services for BPlusTree, useful when testing :)
/// </summary>
partial class BTreeMemoryStore : INodeStorage
{
    private static readonly StorageHandle InvalidRoot = new(uint.MaxValue, uint.MaxValue);
    readonly ConcurrentDictionary<StorageHandle, object> _nodes;

    StorageHandle _root = InvalidRoot;

    /// <summary> Default in-memory storage </summary>
    public BTreeMemoryStore()
    {
        _nodes = new();
	}

    public void Dispose()
    {
        _root = InvalidRoot;
		_nodes.Clear();
	}
    
    public StorageHandle OpenRoot(out bool isNew)
    {
        if (_root == InvalidRoot)
        {
            isNew = true;
			_root = new StorageHandle(0);
		}
        else
            isNew = false;

        return _root;
    }

    public void Reset()
    {
        _root = InvalidRoot;
        _nodes.Clear();
    }

    public bool TryGetNode<TNode>(StorageHandle handle, out TNode node, ISerializer<TNode> _)
    {
        if (_nodes.TryGetValue(handle, out var n))
        {
			node  = (TNode)n;
            return true;
		}

        node = default;
        return false;
	}

    [Obsolete("Not supported", true)]
    void ISerializer<StorageHandle>.WriteTo(StorageHandle handle, IBufferWriter<byte> stream)
    { 
        throw new NotSupportedException(); 
    }

    [Obsolete("Not supported", true)]
    StorageHandle ISerializer<StorageHandle>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
    { 
        throw new NotSupportedException(); 
    }

    public StorageHandle Create()
    {
        return new StorageHandle(0); 
    }

    public void Destroy(StorageHandle handle)
    {
        _nodes.TryRemove(handle, out _);
    }

    public void Update<TNode>(StorageHandle handle, ISerializer<TNode> serializer, TNode node)
    {
        _nodes.AddOrUpdate(handle, node, (k,v) => node);
    }
}
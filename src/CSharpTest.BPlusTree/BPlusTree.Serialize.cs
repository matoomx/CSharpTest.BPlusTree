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

partial class BPlusTree<TKey, TValue>
{
    sealed class NodeSerializer : ISerializer<Node>
    {
        readonly ISerializer<int> _intSerializer = PrimitiveSerializer.Instance;
        readonly ISerializer<bool> _boolSerializer = PrimitiveSerializer.Instance;
        private readonly ISerializer<StorageHandle> _storageHandleSerializer;
        private readonly NodeHandleSerializer _handleSerializer;
        private readonly Options _options;
        private readonly ISerializer<TKey> _keySerializer;
        private readonly ISerializer<TValue> _valueSerializer;

        public NodeSerializer(Options options, NodeHandleSerializer handleSerializer)
        {
            _options = options;
            _keySerializer = options.KeySerializer;
            _valueSerializer = options.ValueSerializer;
            _handleSerializer = handleSerializer;
            _storageHandleSerializer = handleSerializer;
        }

        void ISerializer<Node>.WriteTo(Node value, IBufferWriter<byte> stream)
        {
            _handleSerializer.WriteTo(value.StorageHandle, stream);

            bool isLeaf = value.IsLeaf;
            int maximumKeys = value.IsRoot ? 1 : (isLeaf ? _options.MaximumValueNodes : _options.MaximumChildNodes);
            Check.Assert(value.Size == maximumKeys, "Wrong size");

            _boolSerializer.WriteTo(isLeaf, stream);
            _boolSerializer.WriteTo(value.IsRoot, stream);
            _intSerializer.WriteTo(value.Count, stream);

            for (int i = 0; i < value.Count; i++)
            {
                Element item = value[i];

                if (i > 0 || isLeaf)
                {
                    _keySerializer.WriteTo(item.Key, stream);
                }
                if (isLeaf)
                {
                    _valueSerializer.WriteTo(item.Payload, stream);
                }
                else
                {
                    _handleSerializer.WriteTo(item.ChildNode, stream);
                }
            }
        }

        Node ISerializer<Node>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
        {
            StorageHandle handle = _storageHandleSerializer.ReadFrom(stream, ref position);

            bool isLeaf = _boolSerializer.ReadFrom(stream, ref position);
            bool isRoot = _boolSerializer.ReadFrom(stream, ref position);
            int count = _intSerializer.ReadFrom(stream, ref position);

            Element[] items = new Element[count];

            for (int i = 0; i < count; i++)
            {
                TKey key = default;

                if (i > 0 || isLeaf)
                    key = _keySerializer.ReadFrom(stream, ref position);
                if (isLeaf)
                    items[i] = new Element(key, _valueSerializer.ReadFrom(stream, ref position));
                else
                    items[i] = new Element(key, _handleSerializer.ReadFrom(stream, ref position));
            }

            int nodeSize = isLeaf ? _options.MaximumValueNodes : _options.MaximumChildNodes;
            Check.Assert<ArgumentOutOfRangeException>(nodeSize >= count);
            Node resurrected = Node.FromElements(handle, isRoot, nodeSize, items);
            return resurrected;
        }
    }
}

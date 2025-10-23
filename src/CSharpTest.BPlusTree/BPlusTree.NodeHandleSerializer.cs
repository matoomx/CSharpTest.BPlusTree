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
	sealed class NodeHandleSerializer : ISerializer<NodeHandle>, ISerializer<IStorageHandle>
    {
        private readonly ISerializer<IStorageHandle> _handleSerializer;

        public NodeHandleSerializer(ISerializer<IStorageHandle> handleSerializer)
        {
            _handleSerializer = handleSerializer;
        }

        public void WriteTo(NodeHandle value, IBufferWriter<byte> stream)
        { _handleSerializer.WriteTo(value.StoreHandle, stream); }

        public NodeHandle ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
        { return new NodeHandle(_handleSerializer.ReadFrom(stream, ref position)); }

        public void WriteTo(IStorageHandle value, IBufferWriter<byte> stream)
        { _handleSerializer.WriteTo(value, stream); }

        IStorageHandle ISerializer<IStorageHandle>.ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position )
        { return _handleSerializer.ReadFrom(stream, ref position); }
    }
}

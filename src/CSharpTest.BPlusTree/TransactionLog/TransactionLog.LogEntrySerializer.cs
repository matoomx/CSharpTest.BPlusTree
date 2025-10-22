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
using System.Buffers;

namespace CSharpTest.Collections.Generic;

public sealed partial class TransactionLog<TKey, TValue>
{
	private class LogEntrySerializer : ISerializer<LogEntry>
    {
        private readonly ISerializer<TKey> _keySerializer;
        private readonly ISerializer<TValue> _valueSerializer;

        public LogEntrySerializer(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
        {
            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;
        }
        public void WriteTo(LogEntry value, IBufferWriter<byte> stream)
        {
            PrimitiveSerializer.Int32.WriteTo(value.TransactionId, stream);
            PrimitiveSerializer.Int16.WriteTo((short)value.OpCode, stream);
            _keySerializer.WriteTo(value.Key, stream);
            if (value.OpCode != OperationCode.Remove)
                _valueSerializer.WriteTo(value.Value, stream);
        }
        public LogEntry ReadFrom(ReadOnlySequence<byte> stream, ref SequencePosition position)
        {
			LogEntry entry = new LogEntry
			{
				TransactionId = PrimitiveSerializer.Int32.ReadFrom(stream, ref position),
				OpCode = (OperationCode)PrimitiveSerializer.Int16.ReadFrom(stream, ref position),
				Key = _keySerializer.ReadFrom(stream, ref position)
			};
			if (entry.OpCode != OperationCode.Remove)
                entry.Value = _valueSerializer.ReadFrom(stream, ref position);
            return entry;
        }
    }
}

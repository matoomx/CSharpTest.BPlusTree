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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{
	[StructLayout(LayoutKind.Auto)]
	private struct InsertionInfo : ICreateOrUpdateValue<TKey, TValue>
    {
        private TValue _value;
        private readonly Converter<TKey, TValue> _factory;
        private readonly KeyValueUpdate<TKey, TValue> _updater;
        private readonly bool _canUpdate;
        public TValue Value { get { return _value; } }
        public bool CreateValue(TKey key, out TValue value)
        {
            if (_factory != null)
            {
                value = _value = _factory(key);
                return true;
            }
            value = _value;
            return true;
        }
        public bool UpdateValue(TKey key, ref TValue value)
        {
            if (!_canUpdate)
                throw new DuplicateKeyException($"Duplicate key: {key}");

            if(_updater != null)
                _value = _updater(key, value);

            if (_valueComparer.Equals(value, _value))
                return false;
            
            value = _value;
            return true;
        }
        public InsertionInfo(Converter<TKey, TValue> factory, KeyValueUpdate<TKey, TValue> updater): this()
        {
            _factory = Check.NotNull(factory);
            _updater = updater;
            _canUpdate = updater != null;
        }
        public InsertionInfo(TValue addValue, KeyValueUpdate<TKey, TValue> updater): this()
        {
            _value = addValue;
            _updater = updater;
            _canUpdate = updater != null;
        }
    }
}

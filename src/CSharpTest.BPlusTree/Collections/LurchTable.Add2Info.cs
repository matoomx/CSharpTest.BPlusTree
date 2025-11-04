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
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

public sealed partial class LurchTable<TKey, TValue>
{
	[StructLayout(LayoutKind.Auto)]
	struct Add2Info : ICreateOrUpdateValue<TKey, TValue>
    {
        readonly bool _hasAddValue;
        readonly TValue _addValue;
        public TValue Value;
        public Converter<TKey, TValue> Create;
        public KeyValueUpdate<TKey, TValue> Update;

        public Add2Info(TValue addValue) : this()
        {
            _hasAddValue = true;
            _addValue = addValue;
        }

        public bool CreateValue(TKey key, out TValue value)
        {
            if (_hasAddValue)
            {
                value = Value = _addValue;
                return true;
            }
            if (Create != null)
            {
                value = Value = Create(key);
                return true;
            }
            value = Value = default;
            return false;
        }

        public bool UpdateValue(TKey key, ref TValue value)
        {
            if (Update == null)
            {
                Value = value;
                return false;
            }

            value = Value = Update(key, value);
            return true;
        }
    }
}

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

using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

public sealed partial class LurchTable<TKey, TValue>
{
	#region Internal Structures

	struct UpdateInfo : ICreateOrUpdateValue<TKey, TValue>
    {
        public TValue Value;
        readonly bool _hasTestValue;
        readonly TValue _testValue;

        public UpdateInfo(TValue expected)
        {
            Value = default;
            _testValue = expected;
            _hasTestValue = true;
        }

        bool ICreateValue<TKey, TValue>.CreateValue(TKey key, out TValue value)
        {
            value = default;
            return false;
        }
        public bool UpdateValue(TKey key, ref TValue value)
        {
            if (_hasTestValue && !EqualityComparer<TValue>.Default.Equals(_testValue, value))
                return false;

            value = Value;
            return true;
        }
    }
    #endregion
}

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
using System.Collections;
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

public sealed partial class LurchTable<TKey, TValue>
{
	/// <summary>
	/// Provides an enumerator that iterates through the collection.
	/// </summary>
	public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly LurchTable<TKey, TValue> _owner;
        private EnumState _state;

        internal Enumerator(LurchTable<TKey, TValue> owner)
        {
            _owner = owner;
            _state = new EnumState();
            _state.Init();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _state.Unlock();
        }

        object IEnumerator.Current { get { return Current; } }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public KeyValuePair<TKey, TValue> Current
        {
            get
            {
                int index = _state.Current;
                if (index <= 0)
                    throw new InvalidOperationException();
                if (_owner._entries == null) 
                    throw new ObjectDisposedException(GetType().Name);

                return new KeyValuePair<TKey, TValue>
                    (
                        _owner._entries[index >> _owner._shift][index & _owner._shiftMask].Key,
                        _owner._entries[index >> _owner._shift][index & _owner._shiftMask].Value
                    );
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        public bool MoveNext()
        {
            return _owner.MoveNext(ref _state);
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        public void Reset()
        {
            _state.Unlock();
            _state.Init();
        }
    }
}

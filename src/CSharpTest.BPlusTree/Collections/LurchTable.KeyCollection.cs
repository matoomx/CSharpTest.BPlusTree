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
	/// Provides the collection of Keys for the LurchTable
	/// </summary>
	public sealed class KeyCollection : ICollection<TKey>
    {
        private readonly LurchTable<TKey, TValue> _owner;

        internal KeyCollection(LurchTable<TKey, TValue> owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
        /// </summary>
        public bool Contains(TKey item)
        {
            return _owner.ContainsKey(item);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        public void CopyTo(TKey[] array, int arrayIndex)
        {
            foreach (var item in _owner)
                array[arrayIndex++] = item.Key;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
        /// </summary>
        public int Count
        {
            get { return _owner.Count; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(_owner);
        }

        /// <summary>
        /// Provides an enumerator that iterates through the collection.
        /// </summary>
        public struct Enumerator : IEnumerator<TKey>
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
            public TKey Current
            {
                get
                {
                    int index = _state.Current;
                    if (index <= 0)
                        throw new InvalidOperationException();
                    if (_owner._entries == null)
                        throw new ObjectDisposedException(GetType().Name);
                    return _owner._entries[index >> _owner._shift][index & _owner._shiftMask].Key;
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
        [Obsolete]
        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
        {
            return new Enumerator(_owner);
        }
        [Obsolete]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_owner);
        }
        [Obsolete]
        bool ICollection<TKey>.IsReadOnly
        {
            get { return true; }
        }
        [Obsolete]
        void ICollection<TKey>.Add(TKey item)
        {
            throw new NotSupportedException();
        }
        [Obsolete]
        void ICollection<TKey>.Clear()
        {
            throw new NotSupportedException();
        }
        [Obsolete]
        bool ICollection<TKey>.Remove(TKey item)
        {
            throw new NotSupportedException();
        }
    }
}

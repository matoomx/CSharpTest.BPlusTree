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

namespace CSharpTest.Collections.Generic;

public sealed partial class BPlusTree<TKey, TValue>
{
    private KeyCollection _keysCollection;

	sealed class KeyCollection : ICollection<TKey>
    {
        private readonly IDictionary<TKey, TValue> _owner;

        public KeyCollection(IDictionary<TKey, TValue> owner)
        {
            _owner = owner;
        }

        public int Count { get { return _owner.Count; } }
        public bool IsReadOnly { get { return true; } }

        public bool Contains(TKey item)
        {
            return _owner.ContainsKey(item);
        }

        public void CopyTo(TKey[] array, int arrayIndex)
        {
            foreach (TKey key in this)
                array[arrayIndex++] = key;
        }

        public IEnumerator<TKey> GetEnumerator() { return new KeyEnumerator(_owner.GetEnumerator()); }
        
        [Obsolete]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

        class KeyEnumerator : IEnumerator<TKey>
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _e;
            public KeyEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> e) { _e = e; }
            public TKey Current { get { return _e.Current.Key; } }
            [Obsolete]
            object System.Collections.IEnumerator.Current { get { return Current; } }
            public void Dispose() { _e.Dispose(); }
            public bool MoveNext() { return _e.MoveNext(); }
            public void Reset() { _e.Reset(); }
        }

        [Obsolete]
        void ICollection<TKey>.Add(TKey item) { throw new NotSupportedException(); }
        [Obsolete]
        void ICollection<TKey>.Clear() { throw new NotSupportedException(); }
        [Obsolete]
        bool ICollection<TKey>.Remove(TKey item) { throw new NotSupportedException(); }
    }
}
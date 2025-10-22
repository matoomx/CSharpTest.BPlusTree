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
	private ValueCollection _valuesCollection;
	class ValueCollection : ICollection<TValue>
    {
        private readonly IDictionary<TKey, TValue> _owner;

        public ValueCollection(IDictionary<TKey, TValue> owner)
        {
            _owner = owner;
        }

        #region ICollection<TKey> Members

        public int Count { get { return _owner.Count; } }
        public bool IsReadOnly { get { return true; } }

        public bool Contains(TValue item)
        {
            var c = EqualityComparer<TValue>.Default;
            foreach (TValue value in this)
                if (c.Equals(item, value))
                    return true;
            return false;
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            foreach (TValue value in this)
                array[arrayIndex++] = value;
        }

        public IEnumerator<TValue> GetEnumerator() { return new ValueEnumerator(_owner.GetEnumerator()); }
        [Obsolete]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }

        class ValueEnumerator : IEnumerator<TValue>
        {
            private readonly IEnumerator<KeyValuePair<TKey, TValue>> _e;
            public ValueEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> e) { _e = e; }
            public TValue Current { get { return _e.Current.Value; } }
            [Obsolete]
            object System.Collections.IEnumerator.Current { get { return Current; } }
            public void Dispose() { _e.Dispose(); }
            public bool MoveNext() { return _e.MoveNext(); }
            public void Reset() { _e.Reset(); }
        }

        [Obsolete]
        void ICollection<TValue>.Add(TValue item) { throw new NotSupportedException(); }
        [Obsolete]
        void ICollection<TValue>.Clear() { throw new NotSupportedException(); }
        [Obsolete]
        bool ICollection<TValue>.Remove(TValue item) { throw new NotSupportedException(); }
        #endregion
    }
}
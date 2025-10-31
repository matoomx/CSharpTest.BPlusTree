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
using System.IO;
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

public partial class OrderedEnumeration<T>
{
	private class OrderedEnumerator : IEnumerator<T>
    {
        private readonly IEnumerable<T> _ordered;
        private IEnumerator<T> _enumerator;
        private readonly IComparer<T> _comparer;
        private readonly DuplicateHandling _duplicateHandling;

        private bool _isValid, _hasNext, _isFirst;
        private T _current;
        private T _next;

        public OrderedEnumerator(
            IEnumerable<T> enumerator,
            IComparer<T> comparer,
            DuplicateHandling duplicateHandling)
        {
            _ordered = enumerator;
            _enumerator = null;
            _comparer = comparer;
            _duplicateHandling = duplicateHandling;
            _isFirst = true;
        }

        public void Dispose()
        {
            _enumerator?.Dispose();
            _enumerator = null;
        }

        public bool MoveNext()
        {
            if (_isFirst)
            {
                _isFirst = false;
                _enumerator = _ordered.GetEnumerator();
                _hasNext = _enumerator.MoveNext();
                if (_hasNext)
                    _next = _enumerator.Current;
            }
            _isValid = _hasNext;
            _current = _next;

            if (!_isValid)
                return false;

// ReSharper disable RedundantBoolCompare
            while ((_hasNext = _enumerator.MoveNext()) == true)
// ReSharper restore RedundantBoolCompare
            {
                _next = _enumerator.Current;
                int cmp = _comparer.Compare(_current, _next);
                if (cmp > 0)
                    throw new InvalidDataException("Enumeration out of sequence.");
                if (cmp != 0 || _duplicateHandling == DuplicateHandling.None)
                    break;

                if (_duplicateHandling == DuplicateHandling.RaisesException)
                    throw new ArgumentException("Duplicate item in enumeration.");
                if (_duplicateHandling == DuplicateHandling.LastValueWins)
                    _current = _next;
            }
            if (!_hasNext)
                _next = default;

            return true;
        }

        public T Current
        {
            get
            {
                if (!_isValid)
                    throw new InvalidOperationException();
                return _current;
            }
        }

        object System.Collections.IEnumerator.Current { get { return Current; } }

        void System.Collections.IEnumerator.Reset()
        { throw new NotSupportedException(); }
    }
}

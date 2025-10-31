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
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

public partial class OrderedEnumeration<T>
{
	private class EnumWithDuplicateKeyHandling : IEnumerable<T>
    {
        private readonly IEnumerable<T> _items;
        private readonly IComparer<T> _comparer;
        private readonly DuplicateHandling _duplicateHandling;

        public EnumWithDuplicateKeyHandling(IEnumerable<T> items, IComparer<T> comparer, DuplicateHandling duplicateHandling)
        {
            _items = items;
            _comparer = comparer;
            _duplicateHandling = duplicateHandling;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new OrderedEnumerator(_items, _comparer, _duplicateHandling);
        }
        [Obsolete]
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

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
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{
	[System.Diagnostics.DebuggerDisplay("RootNode, Handle = {_handle}")]
	sealed class RootNode : Node
    {
        public RootNode(IStorageHandle handle) : base(handle, 1)
        { 
            _count = 1; /*invariant for root*/
            _ltype = LockType.Read; /*will be a transacted update, not a create*/
        }
        private RootNode(Node copyFrom, LockType type) : base(copyFrom, type)
        { }

        public override bool IsRoot { get { return true; } }

        public override bool BinarySearch(IComparer<Element> comparer, Element find, out int ordinal)
        { 
            ordinal = 0; 
            return true; 
        }

        public override Node CloneForWrite(LockType ltype)
        {
            if (_ltype == ltype) 
                return this;
            
            return new RootNode(this, ltype);
        }
    }
}

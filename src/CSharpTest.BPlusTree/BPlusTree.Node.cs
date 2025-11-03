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

partial class BPlusTree<TKey, TValue>
{
	class Node
    {
        private readonly StorageHandle _handle;
        protected readonly Element[] _items;
        protected LockType _ltype;
        protected int _count;
        protected int _version;

        public Node(StorageHandle handle, int elementCount)
        {
            _handle = handle;
            _items = new Element[elementCount];
            _ltype = LockType.Insert;
            _count = 0;
        }

        protected Node(Node copyFrom, LockType type)
        {
            _handle = copyFrom._handle;
            _items = (Element[])copyFrom._items.Clone();
            _count = copyFrom._count;
            _ltype = type;
            if (_ltype == LockType.Update && !IsLeaf)
                _ltype = LockType.Read;

            _version = copyFrom._version + 1;
        }

        public static Node FromElements(StorageHandle handle, bool isRoot, int nodeSize, Element[] items)
        {
            if( isRoot)
            {
                RootNode root = new RootNode(handle);
                Check.Assert(items.Length == 1, "Wrong element count for root");
                root._items[0] = items[0];
                return root;
            }

            Node node = new Node(handle, nodeSize);
            Array.Copy(items, 0, node._items, 0, items.Length);
            node._count = items.Length;
            node._ltype = LockType.Read;
            return node;
        }

        public StorageHandle StorageHandle { get { return _handle; } }
        public bool IsReadOnly { get { return _ltype == LockType.Read; } }

        //public void Invalidate()
        //{
        //    _count = int.MinValue;
        //    Array.Clear(_list, 0, _list.Length);
        //    _ltype = LockType.Read;
        //}

        public Node ToReadOnly()
        {
            Check.Assert(_ltype != LockType.Read, "Node is already read-only.");
            _ltype = LockType.Read;
            return this;
        }

        public virtual Node CloneForWrite(LockType ltype)
        {
            if (_ltype == ltype) 
                return this;
            
            Check.Assert(ltype != LockType.Read, "Read lock can not clone for write");
            return new Node(this, ltype);
        }

        public Element this[int ordinal] { get { return _items[ordinal]; } }

        public int Count { get { return _count; } }

        public int Size { get { return _items.Length; } }

        public bool IsLeaf { get { return _count == 0 || _items[0].IsNode == false; } }
        
        public virtual bool IsRoot { get { return false; } }

        public virtual bool BinarySearch(IComparer<Element> comparer, Element find, out int ordinal)
        {
            int start = _count == 0 || _items[0].IsValue ? 0 : 1;
            ordinal = Array.BinarySearch(_items, start, _count - start, find, comparer);
            if (ordinal < 0)
            {
                ordinal = ~ordinal;
                if (IsLeaf)
                    return false;

                if (ordinal > 0)
                    ordinal--;
                return false;
            }
            return true;
        }

        public virtual bool BinarySearch<TAlternate>(IAlternateComparer<TAlternate, TKey> comparer, TAlternate altKey, out int ordinal) where TAlternate : allows ref struct
		{             
            int start = _count == 0 || _items[0].IsValue ? 0 : 1;

            ordinal = BinarySearch(_items, start, _count - start, altKey, comparer);
            
            if (ordinal < 0)
            {
                ordinal = ~ordinal;
                if (IsLeaf)
                    return false;
                if (ordinal > 0)
                    ordinal--;
                return false;
            }
            return true;
		}

        private static int BinarySearch<TAlternate>(Element[] source, int index, int length, TAlternate altKey, IAlternateComparer<TAlternate, TKey> comparer) where TAlternate : allows ref struct
		{
			int lo = index;
			int hi = index + length - 1;

			while (lo <= hi)
			{
				int i = lo + ((hi - lo) >> 1); 
				int c = comparer.Compare(source[i].Key, altKey);

				if (c == 0) return i;
				if (c < 0)
					lo = i + 1;
				else
					hi = i - 1;
			}
			return ~lo;
		}


		public void ReplaceKey(int ordinal, TKey minKey)
        { 
            ReplaceKey(ordinal, minKey, null); 
        }
        
        public void ReplaceKey(int ordinal, TKey minKey, IComparer<TKey> comparer)
        {
            Check.Assert(!IsRoot, "Invalid operation on root.");
            Check.Assert(_ltype != LockType.Read, "Node is currently read-only");
            Check.Assert(ordinal >= 0 && ordinal < _count, "Index out of range.");

            if (comparer == null || comparer.Compare(minKey, _items[ordinal].Key) != 0)
                _items[ordinal] = new Element(minKey, _items[ordinal]);
        }

        public void ReplaceChild(int ordinal, NodeHandle original, NodeHandle value)
        {
            Check.Assert(_ltype != LockType.Read, "Node is currently read-only");
            Check.Assert(ordinal >= 0 && ordinal < _count, "Index out of range.");
            Element replacing = _items[ordinal];
            Check.Assert(
                (original == null && replacing.ChildNode == null) ||
                (original != null && original.Equals(replacing.ChildNode))
                , "Incorrect child being replaced.");
            _items[ordinal] = new Element(replacing.Key, value);
        }

        public void SetValue(int ordinal, TKey key, TValue value, IComparer<TKey> comparer)
        {
            Check.Assert(!IsRoot, "Invalid operation on root.");
            Check.Assert(_ltype != LockType.Read, "Node is currently read-only");
            Check.Assert(ordinal >= 0 && ordinal < _count, "Index out of range.");
            Check.Assert(comparer.Compare(_items[ordinal].Key, key) == 0, "Incorrect key for value replacement.");
            _items[ordinal] = new Element(key, value);
        }

        public void Insert(int ordinal, Element item)
        {
            Check.Assert(!IsRoot, "Invalid operation on root.");
            Check.Assert(_ltype != LockType.Read, "Node is currently read-only");
            if (ordinal < 0 || ordinal > _count || ordinal >= _items.Length)
                throw new ArgumentOutOfRangeException(nameof(ordinal));

            if (ordinal < _count)
                Array.Copy(_items, ordinal, _items, ordinal + 1, _count - ordinal);

            _items[ordinal] = item;

            _count++;
        }

        public void Remove(int ordinal, Element item, IComparer<TKey> comparer)
        {
            Check.Assert(!IsRoot, "Invalid operation on root.");
            Check.Assert(_ltype != LockType.Read, "Node is currently read-only");
            
            if (ordinal < 0 || ordinal >= _count)
				throw new ArgumentOutOfRangeException(nameof(ordinal));

			Check.Assert<InvalidOperationException>(comparer.Compare(_items[ordinal].Key, item.Key) == 0);

            if (ordinal < _count - 1)
                Array.Copy(_items, ordinal + 1, _items, ordinal, _count - ordinal - 1);

            _count--;
            _items[_count] = new Element();
        }

        /// <summary> For enumeration </summary>
        public void CopyTo(Element[] elements, out int currentLimit)
        {
            _items.CopyTo(elements, 0);
            currentLimit = _count;
        }
    }
}

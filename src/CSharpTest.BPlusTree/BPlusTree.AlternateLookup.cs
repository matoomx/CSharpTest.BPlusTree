using System;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{
	public AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey>() where TAlternateKey : allows ref struct
	{
		return new AlternateLookup<TAlternateKey>(this);
	}

	public readonly struct AlternateLookup<TAlternateKey> where TAlternateKey : allows ref struct
	{
		readonly IAlternateComparer<TAlternateKey, TKey> _comparer;

		internal AlternateLookup(BPlusTree<TKey, TValue> dictionary)
		{
			BPlusTree = dictionary;
			_comparer = (IAlternateComparer<TAlternateKey, TKey>)dictionary._keyComparer;

			if (_comparer == null)
				throw new InvalidOperationException("The BPlusTree was not constructed with an IAlternateComparer thst is comatible.");

		}

		public BPlusTree<TKey, TValue> BPlusTree { get; }

		public TValue this[TAlternateKey key]
		{
			get
			{
				if (!TryGetValue(key, out TValue result))
					throw new IndexOutOfRangeException();

				return result;
			}
		}

		public bool Contains(TAlternateKey key)
		{
			using var root = BPlusTree.LockRoot(LockType.Read);
			return Seek(root.Pin, key, out _, out _);
		}

		public bool TryGetValue(TAlternateKey key, out TValue value)
		{
			using var root = BPlusTree.LockRoot(LockType.Read);
			return Search(root.Pin, key, out _, out value);
		}

		public bool TryGetValue(TAlternateKey key, out TKey originalKey, out TValue value)
		{
			using RootLock root = BPlusTree.LockRoot(LockType.Read);
			return Search(root.Pin, key, out originalKey, out value);
		}

		private bool Search(NodePin thisLock, TAlternateKey key, out TKey originalKey, out TValue value)
		{
			if (Seek(thisLock, key, out NodePin pin, out int offset))
				using (pin)
				{
					var element = pin.Ptr[offset];
					originalKey = element.Key;
					value = element.Payload;
					return true;
				}

			originalKey = default;
			value = default;
			return false;
		}

		private bool Seek(NodePin thisLock, TAlternateKey key, out NodePin pin, out int offset)
		{
			NodePin myPin = thisLock, nextPin = null;
			try
			{
				while (myPin != null)
				{
					Node me = myPin.Ptr;

					bool isValueNode = me.IsLeaf;
					if (me.BinarySearch( _comparer, key, out int ordinal) && isValueNode)
					{
						pin = myPin;
						myPin = null;
						offset = ordinal;
						return true;
					}
					if (isValueNode)
						break; // not found.

					nextPin = BPlusTree._storage.Lock(myPin, me[ordinal].ChildNode);
					myPin.Dispose();
					myPin = nextPin;
					nextPin = null;
				}
			}
			finally
			{
				myPin?.Dispose();
				nextPin?.Dispose();
			}

			pin = null;
			offset = -1;
			return false;
		}
	}
}
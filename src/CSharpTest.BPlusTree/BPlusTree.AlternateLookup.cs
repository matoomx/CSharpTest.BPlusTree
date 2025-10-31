using System;
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;


public interface IAlternateComparer<in TAlternate, TKey> where TAlternate : allows ref struct where TKey : allows ref struct
{
	int Compare(TKey x, TAlternate y);
}

partial class BPlusTree<TKey, TValue>
{
	public AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey>(IAlternateComparer<TAlternateKey, TKey> comparer) where TAlternateKey : allows ref struct
	{
		ArgumentNullException.ThrowIfNull(comparer, nameof(comparer));
		return new AlternateLookup<TAlternateKey>(this, comparer);
	}

	public readonly struct AlternateLookup<TAlternateKey> where TAlternateKey : allows ref struct
	{
		readonly IAlternateComparer<TAlternateKey, TKey> _comparer;

		internal AlternateLookup(BPlusTree<TKey, TValue> dictionary, IAlternateComparer<TAlternateKey, TKey> comparer)
		{
			BPlusTree = dictionary;
			_comparer = comparer;
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

		public bool TryGetValue(TAlternateKey key, out TValue value)
		{
			bool result;
			value = default;

			using (RootLock root = BPlusTree.LockRoot(LockType.Read))
				result = Search(root.Pin, key, ref value);

			return result;
		}

		private bool Search(NodePin thisLock, TAlternateKey key, ref TValue value)
		{
			if (Seek(thisLock, key, out NodePin pin, out int offset))
				using (pin)
				{
					value = pin.Ptr[offset].Payload;
					return true;
				}
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
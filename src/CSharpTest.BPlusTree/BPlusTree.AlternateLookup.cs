using System;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{

	/// <summary>
	/// Altrnate key lookup support. The BPlusTree must have been constructed with an IAlternateComparer that supports the specified alternate key type.
	/// </summary>
	/// <typeparam name="TAlternateKey"></typeparam>
	/// <returns></returns>
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

		/// <summary>
		/// The BPlusTree instance this lookup is associated with.
		/// </summary>
		public BPlusTree<TKey, TValue> BPlusTree { get; }

		
		/// <summary>
		/// Gets the value associated with the specified alternate key.
		/// </summary>
		/// <param name="key">The alternate key whose associated value is to be retrieved.</param>
		/// <returns>The value associated with the specified key.</returns>
		/// <exception cref="IndexOutOfRangeException">Thrown if the specified <paramref name="key"/> does not exist in the collection.</exception>
		public TValue this[TAlternateKey key]
		{
			get
			{
				if (!TryGetValue(key, out TValue result))
					throw new IndexOutOfRangeException();

				return result;
			}
		}

		/// <summary>
		/// Determines whether the specified key exists in the BPlusTree.
		/// </summary>
		/// <param name="key">The alternate key to locate in the BPlusTree.</param>
		/// <returns><see langword="true"/> if the specified key exists in the BPlusTree; otherwise, <see langword="false"/>.</returns>
		public bool Contains(TAlternateKey key)
		{
			using var root = BPlusTree.LockRoot(LockType.Read);
			return Seek(root.Pin, key, out _, out _);
		}

		/// <summary>
		/// Attempts to retrieve the value associated with the specified key.
		/// </summary>
		/// <param name="key">The key to locate in the collection.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise,
		/// the default value for the type of the <typeparamref name="TValue"/> parameter.</param>
		/// <returns><see langword="true"/> if the key was found and the value was successfully retrieved; otherwise, <see
		/// langword="false"/>.</returns>
		public bool TryGetValue(TAlternateKey key, out TValue value)
		{
			using var root = BPlusTree.LockRoot(LockType.Read);
			return Search(root.Pin, key, out _, out value);
		}

		/// <summary>
		/// Attempts to retrieve the original key and associated value for the specified alternate key.
		/// </summary>
		/// <param name="key">The alternate key to search for in the tree.</param>
		/// <param name="originalKey">When this method returns, contains the original key associated with the specified alternate key, if the search was
		/// successful; otherwise, the default value for the type of the original key.</param>
		/// <param name="value">When this method returns, contains the value associated with the specified alternate key, if the search was
		/// successful; otherwise, the default value for the type of the value.</param>
		/// <returns><see langword="true"/> if the specified alternate key was found and the original key and value were successfully
		/// retrieved; otherwise, <see langword="false"/>.</returns>
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
using System;
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

public static class BPlusTree
{
	/// <summary>
	/// Creates a BPlusTree instance with Memory storage and the selected serializers.
	/// </summary>
	public static BPlusTree<TKey, TValue> Create<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, IComparer<TKey> comparer = null)
	{
		ArgumentNullException.ThrowIfNull(keySerializer, nameof(keySerializer));
		ArgumentNullException.ThrowIfNull(valueSerializer, nameof(valueSerializer)); 
		return new BPlusTree<TKey, TValue>(new BPlusTree<TKey, TValue>.Options(keySerializer, valueSerializer, comparer ?? GetDefaultComparer<TKey>()));
	}

	/// <summary>
	/// Creates a BPlusTree instance with disk storage and the selected serializers.
	/// </summary>
	public static BPlusTree<TKey, TValue> Create<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, string fileName)
	{
		return Create(keySerializer, valueSerializer, GetDefaultComparer<TKey>(), fileName, 0, 0);
	}

	/// <summary>
	/// Creates a BPlusTree instance with disk storage and the selected serializers.
	/// </summary>
	public static BPlusTree<TKey, TValue> Create<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, IComparer<TKey> comparer, string fileName)
	{
		return Create(keySerializer, valueSerializer, comparer, fileName, 0, 0);
	}

	/// <summary>
	/// Creates a BPlusTree instance with disk storage and the selected serializers. The storage is optimized for the selected key and value sizes (in bytes)
	/// </summary>
	public static BPlusTree<TKey, TValue> Create<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, string fileName, int averageKeySizeBytes, int averageValueSizeBytes)
	{
		return Create(keySerializer, valueSerializer, GetDefaultComparer<TKey>(), fileName, averageKeySizeBytes, averageValueSizeBytes);
	}

	/// <summary>
	/// Creates a BPlusTree instance with disk storage and the selected serializers. The storage is optimized for the selected key and value sizes (in bytes)
	/// </summary>
	public static BPlusTree<TKey, TValue> Create<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, IComparer<TKey> comparer, string fileName, int averageKeySizeBytes, int averageValueSizeBytes)
	{
		ArgumentNullException.ThrowIfNull(keySerializer, nameof(keySerializer));
		ArgumentNullException.ThrowIfNull(valueSerializer, nameof(valueSerializer));
		ArgumentNullException.ThrowIfNullOrEmpty(fileName, nameof(fileName));

		var options = new BPlusTree<TKey, TValue>.Options(keySerializer, valueSerializer, comparer)
		{
			FileName = fileName,
			CreateFile = CreatePolicy.IfNeeded
		};

		if (averageKeySizeBytes > 0 && averageValueSizeBytes > 0)
			options.CalculateOrder(averageKeySizeBytes, averageValueSizeBytes);

		return new BPlusTree<TKey, TValue>(options);
	}


	/// <summary>
	/// Creates a BPlusTree instance with the selected options.
	/// </summary>
	public static BPlusTree<TKey, TValue> Create<TKey, TValue>(BPlusTree<TKey, TValue>.Options options)
	{
		return new BPlusTree<TKey, TValue>(options);
	}

	/// <summary>
	/// Creates a BPlusTree OPtions instance with selected serializers.
	/// </summary>
	public static BPlusTree<TKey, TValue>.Options CreateOptions<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, IComparer<TKey> comparer = null)
	{
		return new BPlusTree<TKey, TValue>.Options(keySerializer, valueSerializer, comparer ?? Comparer<TKey>.Default);
	}

	/// <summary>
	/// Directly enumerates the contents of BPlusTree from disk in read-only mode.
	/// </summary>
	/// <param name="options"> The options normally used to create the <see cref="BPlusTree{TKey, TValue}"/> instance </param>
	/// <returns> Yields the Key/Value pairs found in the file </returns>
	public static IEnumerable<KeyValuePair<TKey,TValue>> EnumerateFile<TKey, TValue>(BPlusTree<TKey, TValue>.Options options)
	{
		return BPlusTree<TKey,TValue>.EnumerateFile(options);
	}

	private static IComparer<TKey> GetDefaultComparer<TKey>()
	{
		return (typeof(TKey) == typeof(string)) ? (IComparer<TKey>)(IComparer<string>)AlternateComparers.StringOrdinal : Comparer<TKey>.Default;
	}
}

using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

public static class BPlusTree
{
	/// <summary>
	/// Creates a BPlusTree instance with the selected serializers.
	/// </summary>
	public static BPlusTree<TKey, TValue> Create<TKey, TValue>(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, IComparer<TKey> comparer = null)
	{
		return new BPlusTree<TKey, TValue>(new BPlusTree<TKey, TValue>.Options(keySerializer, valueSerializer, comparer ?? Comparer<TKey>.Default));
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
}

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
using System.Buffers.Binary;
using Microsoft.Win32.SafeHandles;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Creates an ordered enumeration from an unordered enumeration by paginating the data, sorting the page,
/// and then performing a binary-tree grouped mergesort on the resulting pages.  When the page size (memoryLimit)
/// is hit, the page will be unloaded to disk and restored on demand if a serializer is provided.
/// </summary>
public partial class OrderedEnumeration<T> : IEnumerable<T>
{
    private const int DefaultLimit = 0x10000;
    private const int LimitMax = int.MaxValue;
    private readonly IEnumerable<T> _unordered;
    private bool _enumerated;

    /// <summary> Constructs an ordered enumeration from an unordered enumeration </summary>
    public OrderedEnumeration(IEnumerable<T> unordered) : this(Comparer<T>.Default, unordered, null, DefaultLimit) { }
    /// <summary> Constructs an ordered enumeration from an unordered enumeration </summary>
    public OrderedEnumeration(IComparer<T> comparer, IEnumerable<T> unordered) : this(comparer, unordered, null, DefaultLimit) { }
    /// <summary> Constructs an ordered enumeration from an unordered enumeration </summary>
    public OrderedEnumeration(IComparer<T> comparer, IEnumerable<T> unordered, ISerializer<T> serializer) : this(comparer, unordered, serializer, DefaultLimit) { }
    /// <summary> Constructs an ordered enumeration from an unordered enumeration </summary>
    public OrderedEnumeration(IComparer<T> comparer, IEnumerable<T> unordered, ISerializer<T> serializer, int memoryLimit)
    {
        _enumerated = false;
        Comparer = comparer;
		_unordered = unordered;
        Serializer = serializer;
        InMemoryLimit = Check.InRange(memoryLimit, 1, LimitMax);
    }

    /// <summary>
    /// Gets the comparer to use when ordering the items.
    /// </summary>
    public IComparer<T> Comparer
    {
        get;init;
	}

    /// <summary>
    /// Gets the serializer to use when paging to disk.
    /// </summary>
    public ISerializer<T> Serializer
    {
        get; init;
    }

    /// <summary>
    /// Gets the number of instances to keep in memory before sorting/paging to disk.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">You must specify the Serializer before setting this property</exception>
    public int InMemoryLimit
    {
        get;init;
	}

    /// <summary> Gets the duplicate item handling policy </summary>
    public DuplicateHandling DuplicateHandling
    {
        get; init;
    } = DuplicateHandling.None;

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">GetEnumerator() may only be called once.</exception>
    /// <exception cref="System.IO.InvalidDataException">Enumeration is out of sequence.</exception>
    /// <exception cref="System.ArgumentException">Duplicate item in enumeration.</exception>
    public IEnumerator<T> GetEnumerator()
    {
        if (_enumerated)
            throw new InvalidOperationException();
        _enumerated = true;
        return new OrderedEnumerator(PagedAndOrdered(), Comparer, DuplicateHandling);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    { return GetEnumerator(); }

    private IEnumerable<T> PagedAndOrdered()
    {
        T[] items = new T[Math.Min(InMemoryLimit, 2048)];
        var orderedSet = new List<IEnumerable<T>>();
        int count = 0;

        foreach (T item in _unordered)
        {
            if (InMemoryLimit > 0 && count == InMemoryLimit)
            {
                if (Serializer != null)
                {
                    var tempFile = Path.GetTempFileName();
					var io = File.OpenHandle(tempFile, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                    long ioPos = 0;

                    MergeSort.Sort(items, Comparer);
                    var buffer = new SerializeStream();
                    foreach (T i in items)
                    {
                        var sizeHeader = buffer.GetSpan(4);
                        buffer.Advance(4);
                        var pos = buffer.Position;
                        Serializer.WriteTo(i, buffer);
                        BinaryPrimitives.WriteInt32LittleEndian(sizeHeader, (int)(buffer.Position - pos));

                        if (buffer.Position >= 4096)
                        {
                            RandomAccess.Write(io, buffer.GetBlocks(), ioPos);
                            ioPos += buffer.Position;
                            buffer.Clear();
                        }
                    }
                    if (buffer.Position > 0)
                        RandomAccess.Write(io, buffer.GetBlocks(), ioPos);

                    orderedSet.Add(Read(io, tempFile));
                }
                else
                {
                    MergeSort.Sort(items, out T[] copy, 0, items.Length, Comparer);
                    orderedSet.Add(items);
                    items = copy;
                }
                Array.Clear(items, 0, items.Length);
                count = 0;
            }

            if (count == items.Length)
                Array.Resize(ref items, Math.Min(InMemoryLimit, items.Length * 2));
            items[count++] = item;
        }

        if (count != items.Length)
            Array.Resize(ref items, count);

        MergeSort.Sort(items, Comparer);

        IEnumerable<T> result;
        if (orderedSet.Count == 0)
            result = items;
        else
        {
            orderedSet.Add(items);
            result = Merge(Comparer, orderedSet.ToArray());
        }

        foreach (T item in result)
            yield return item;
    }

    private IEnumerable<T> Read(SafeFileHandle io, string fileName)
    {
        try
        {
            using var buffer = new DeserializeStream(io);

            for (int i = 0; i < InMemoryLimit; i++)
            {
				var data = buffer.Read();
				var pos = data.Start;
				yield return Serializer.ReadFrom(data, ref pos);
            }
		}
        finally
        {
            io.Dispose();
            File.Delete(fileName);
		}
	}

	/// <summary>
	/// Merges two ordered enumerations based on the comparer provided.
	/// </summary>
	public static IEnumerable<T> Merge(IComparer<T> comparer, IEnumerable<T> x, IEnumerable<T> y)
    {
		using IEnumerator<T> left = x.GetEnumerator();
		using IEnumerator<T> right = y.GetEnumerator();
		bool lvalid = left.MoveNext();
		bool rvalid = right.MoveNext();
		while (lvalid || rvalid)
		{
			int cmp = !rvalid ? -1 : !lvalid ? 1 : comparer.Compare(left.Current, right.Current);
			if (cmp <= 0)
			{
				yield return left.Current;
				lvalid = left.MoveNext();
			}
			else
			{
				yield return right.Current;
				rvalid = right.MoveNext();
			}
		}
	}
    /// <summary>
    /// Merges n-number of ordered enumerations based on the default comparer of T.
    /// </summary>
    public static IEnumerable<T> Merge(params IEnumerable<T>[] enums)
    {
        return Merge(Comparer<T>.Default, 0, enums.Length, enums);
    }
    /// <summary>
    /// Merges n-number of ordered enumerations based on the comparer provided.
    /// </summary>
    public static IEnumerable<T> Merge(IComparer<T> comparer, params IEnumerable<T>[] enums)
    {
        return Merge(comparer, 0, enums.Length, enums);
    }
    /// <summary>
    /// Merges n-number of ordered enumerations based on the comparer provided.
    /// </summary>
    public static IEnumerable<T> Merge(IComparer<T> comparer, DuplicateHandling duplicateHandling, params IEnumerable<T>[] enums)
    {
        return WithDuplicateHandling(Merge(comparer, enums), comparer, duplicateHandling);
    }

    private static IEnumerable<T> Merge(IComparer<T> comparer, int start, int count, IEnumerable<T>[] enums)
    {
        if (count <= 0)
            return [];
        if (count == 1)
            return enums[start];
        if (count == 2)
            return Merge(comparer, enums[start], enums[start + 1]);

        int half = count/2;
        return Merge(comparer, 
            Merge(comparer, start, half, enums),
            Merge(comparer, start + half, count - half, enums)
        );
    }

    /// <summary>
    /// Wraps an existing enumeration of Key/value pairs with an assertion about ascending order and handling
    /// for duplicate keys.
    /// </summary>
    public static IEnumerable<T> WithDuplicateHandling(
        IEnumerable<T> items, IComparer<T> comparer, DuplicateHandling duplicateHandling)
    {
        return new EnumWithDuplicateKeyHandling(items, comparer, duplicateHandling);
    }
}

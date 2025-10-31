using System;
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

public interface IAlternateComparer<in TAlternate, TKey> where TAlternate : allows ref struct where TKey : allows ref struct
{
	int Compare(TKey x, TAlternate y);
}

public sealed class StringOrdinalIgnoreCase : IComparer<string>, IAlternateComparer<ReadOnlySpan<char>, string>
{
	public int Compare(string x, string y)
	{
		return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
	}

	public int Compare(string x, ReadOnlySpan<char> y)
	{
		return x.AsSpan().CompareTo(y, StringComparison.OrdinalIgnoreCase);
	}
}

public sealed class StringOrdinal : IComparer<string>, IAlternateComparer<ReadOnlySpan<char>, string>
{
	public int Compare(string x, string y)
	{
		return string.Compare(x, y, StringComparison.Ordinal);
	}

	public int Compare(string x, ReadOnlySpan<char> y)
	{
		return x.AsSpan().CompareTo(y, StringComparison.Ordinal);
	}
}

public static class AlternateComparers
{
	public static readonly StringOrdinalIgnoreCase StringOrdinalIgnoreCase = new();
	public static readonly StringOrdinal StringOrdinal = new();
}

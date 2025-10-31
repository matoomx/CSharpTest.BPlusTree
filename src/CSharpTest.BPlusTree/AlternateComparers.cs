using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpTest.Collections.Generic;

public interface IAlternateComparer<in TAlternate, TKey> where TAlternate : allows ref struct where TKey : allows ref struct
{
	int Compare(TKey x, TAlternate y);
}

public sealed class StringAlternateOrdinalIgnoreCase : IAlternateComparer<ReadOnlySpan<char>, string>
{
	public int Compare(string x, ReadOnlySpan<char> y)
	{
		return x.AsSpan().CompareTo(y, StringComparison.OrdinalIgnoreCase);
	}
}

public sealed class StringAlternateOrdinal : IAlternateComparer<ReadOnlySpan<char>, string>
{
	public int Compare(string x, ReadOnlySpan<char> y)
	{
		return x.AsSpan().CompareTo(y, StringComparison.Ordinal);
	}
}

public static class AlternateComparers
{
	public static readonly StringAlternateOrdinalIgnoreCase StringOrdinalIgnoreCase = new();
	public static readonly StringAlternateOrdinal StringOrdinal = new();
}

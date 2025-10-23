using BenchmarkDotNet.Running;

namespace CSharpTest.Benchmark;

internal class Program
{
	static void Main(string[] _)
	{
		BenchmarkRunner.Run<PreForkCompare>();
	}
}

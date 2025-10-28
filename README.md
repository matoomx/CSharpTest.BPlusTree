[![NuGet](https://img.shields.io/nuget/v/CSharpTest.BPlusTree.svg?color=blue)](https://www.nuget.org/packages/CSharpTest.BPlusTree)
[![CI](https://github.com/matoomx/CSharpTest.BPlusTree/actions/workflows/dotnet.yml/badge.svg)](https://github.com/matoomx/CSharpTest.BPlusTree/actions/workflows/dotnet.yml)

CSharpTest.BPlusTree
=======================

## Background ## 

I first came across this code when I had the same thoughts as CSharpTest http://csharptest.net/482/building-a-database-in-c-part-1/index.html about persisting data from a dictionary in a project. The implementation of b+tree with a IDictionary<TKey, TValue> interface seemed perfect, but the requirements changed, and I didn’t end up using CSharpTest.Net.Collections. Now I have once again stumbled upon a solution that may benefit from this library and I looked at it once again but since it is unmaintained I wanted to test if I could simplify it for my needs. I also wanted to test if I could update the code to use new .net APIs that has been released after the original implementation. The major question was, could the library get increased performance with new APIs like RandomAccess and System.Buffers? After some testing on the code the answer seems to be yes.

I have since ported many of the original tests and one benchmark to prove the new implementation to the point that I now don’t know of any issues.

Some of the changes in this fork...

* A New storage based upon the v2 format, but not compatible so datafiles needs to be recreated.
* Use .net 9.
* Remove all non BPulsTree relevant code.
* Change Serialization interface to use IBufferWriter and ReadOnlySequence.
* Use BinaryPrimitives in primitive serializer.
* Use RandomAccess for file io.
* Use a single test project based on MSTest since I'm used to that.
* Code restructure (I went too far but had to try to find what was really needed).
* Remove RecycableMemoryStream and use rented buffers instead.
* Use System.IO.Hashing for vectorized CRC checks.
* Use a single namespace CSharpTest.Collections.Generic so it's easier for clients.
* More statics so that generic types can be inferred.  

## Performance ##

The new serialization format and the more direct file access uses less memory and can increase performance. Here are some numbers from my computer.

| Method                     | Mean    | Error    | StdDev   | Median  | Ratio | RatioSD | Gen0        | Gen1       | Allocated  | Alloc Ratio |
|--------------------------- |--------:|---------:|---------:|--------:|------:|--------:|------------:|-----------:|-----------:|------------:|
| Original_RawDisk_IntString | 3.228 s | 0.0563 s | 0.0526 s | 3.223 s |  1.00 |    0.02 | 147000.0000 | 71000.0000 | 1179.57 MB |        1.00 |
| Fork_RawDisk_IntString     | 2.470 s | 0.0490 s | 0.0957 s | 2.430 s |  0.77 |    0.03 |  30000.0000 | 12000.0000 |  242.34 MB |        0.21 |

| Method                   | Mean    | Error    | StdDev   | Ratio | RatioSD | Gen0       | Gen1      | Allocated | Alloc Ratio |
|------------------------- |--------:|---------:|---------:|------:|--------:|-----------:|----------:|----------:|------------:|
| Original_RawDisk_IntGuid | 1.977 s | 0.0378 s | 0.0371 s |  1.00 |    0.03 | 20000.0000 | 6000.0000 | 166.69 MB |        1.00 |
| Fork_RawDisk_IntGuid     | 1.595 s | 0.0329 s | 0.0960 s |  0.81 |    0.05 |  4000.0000 | 1000.0000 |  34.52 MB |        0.21 |


## Online Help for the original implementation ##

BPlusTree Help: http://help.csharptest.net/?CSharpTest.Net.BPlusTree~CSharpTest.Net.Collections.BPlusTree%602.html

## Quick start ##


### BPlusTree Example ###
```
var dataFile = Path.GetTempFileName();
var tempDir = new DirectoryInfo(Path.GetTempPath());

//Create a BPlusTree with with all temp files and when they where updated and use dataFile as storage. 
using (var tree = BPlusTree.Create(PrimitiveSerializer.String, PrimitiveSerializer.DateTime, dataFile))
{
	foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
		tree.Add(file.FullName, file.LastWriteTimeUtc);
}

//Create a new BPlusTree with the previously created dataFile and check what files have changed.
using (var tree = BPlusTree.Create(PrimitiveSerializer.String, PrimitiveSerializer.DateTime, dataFile))
{
	foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
	{
		if (!tree.TryGetValue(file.FullName, out DateTime cmpDate))
			Console.WriteLine("New file: {0}", file.FullName);
		else if (cmpDate != file.LastWriteTimeUtc)
			Console.WriteLine("Modified: {0}", file.FullName);
		tree.Remove(file.FullName);
	}
	foreach (var item in tree)
		Console.WriteLine("Removed: {0}", item.Key);
}
```

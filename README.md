[![CI](https://github.com/matoomx/CSharpTest.BPlusTree/actions/workflows/dotnet.yml/badge.svg)](https://github.com/matoomx/CSharpTest.BPlusTree/actions/workflows/dotnet.yml)

CSharpTest.Net.Collections
=======================

CSharpTest.Net.Collections (moved from https://code.google.com/p/csharptest-net/)

## Change Log ##

2014-09-06	Initial clone and extraction from existing library.

2025-10-22	Fork to reintegrate offline BPlusTree modifications. This will not be compatible with existing data files and use a new storage based upon the v2 format. Some of the changes...
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

## Online Help ##

BPlusTree Help: http://help.csharptest.net/?CSharpTest.Net.BPlusTree~CSharpTest.Net.Collections.BPlusTree%602.html

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


## Quick start ##


### BPlusTree Example ###
```
var fileName = Path.GetTempFileName();
using (var tree = BPlusTree.Create(PrimitiveSerializer.String, PrimitiveSerializer.DateTime, fileName))
{
	var tempDir = new DirectoryInfo(Path.GetTempPath());
	foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
		tree.Add(file.FullName, file.LastWriteTimeUtc);
}

using (var tree = BPlusTree.Create(PrimitiveSerializer.String, PrimitiveSerializer.DateTime, fileName))
{
	var tempDir = new DirectoryInfo(Path.GetTempPath());
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

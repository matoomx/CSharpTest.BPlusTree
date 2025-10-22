CSharpTest.Net.Collections
=======================

CSharpTest.Net.Collections (moved from https://code.google.com/p/csharptest-net/)

## Change Log ##

2014-09-06	Initial clone and extraction from existing library.

2025-10-22	Fork to reintegrate offline BPlusTree modifications. This will not be compatible with existing data files and use a new storage based upon the v2 format. Some of the changes...
Use .net 9.
Remove all non BPulsTree relevant code.
Change Serialization interface to use IBufferWriter and ReadOnlySequence.
Use BinaryPrimitives in primitive serializer.
Use RandomAccess for file io.
Use a single test project based on MSTest since im used to that.
Code restructure (I went too far but had to try to find what was really needed).
Remove RecycableMemoryStream and use rented buffers instead.
Use System.IO.Hashing for vectorized CRC checks.
Use a single namespace CSharpTest.Collections.Generic so it's easier for clients.
More statics so that generic types can be inferred.  

## Online Help ##

BPlusTree Help: http://help.csharptest.net/?CSharpTest.Net.BPlusTree~CSharpTest.Net.Collections.BPlusTree%602.html

## Quick start ##


### BPlusTree Example ###
```
var options = BPlusTree.CreateOptions(PrimitiveSerializer.String, PrimitiveSerializer.DateTime);	
options.CalcBTreeOrder(16, 24);
options.CreateFile = CreatePolicy.Always;
options.FileName = Path.GetTempFileName();
using (var tree = BPlusTree.Create(options))
{
    var tempDir = new DirectoryInfo(Path.GetTempPath());
    foreach (var file in tempDir.GetFiles("*", SearchOption.AllDirectories))
        tree.Add(file.FullName, file.LastWriteTimeUtc);
}
options.CreateFile = CreatePolicy.Never;
using (var tree = BPlusTree.Create(options))
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

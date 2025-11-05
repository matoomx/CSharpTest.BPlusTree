using System.IO;
using BenchmarkDotNet.Attributes;
using PreForkCollection = CSharpTest.Net.Collections;
using PreForkSerialization = CSharpTest.Net.Serialization;
using CSharpTest.Collections.Generic;
using System;

namespace CSharpTest.Benchmark;

[MemoryDiagnoser]
public class PreForkCompare
{
	[Benchmark]
	public void Original_IntString()
    {
        var options = new PreForkCollection.BPlusTree<int, string>.OptionsV2(PreForkSerialization.PrimitiveSerializer.Int32, PreForkSerialization.PrimitiveSerializer.String)
        {
            CreateFile = PreForkCollection.CreatePolicy.Always,
            FileName = Path.GetTempFileName(),
			CachePolicy = PreForkCollection.CachePolicy.None,
			StoragePerformance = PreForkCollection.StoragePerformance.CommitToDisk,
		};

        const int entries = 5000;

        using var tree = new PreForkCollection.BPlusTree<int, string>(options);

        for (var i = 0; i < entries; i++)
			tree[i] = Lipsum.Copy;
	}

	[Benchmark]
	public void Fork_IntString()
	{
		var options = new BPlusTree<int, string>.Options(PrimitiveSerializer.Int32, PrimitiveSerializer.String)
		{
			CreateFile = CreatePolicy.Always,
			FileName = Path.GetTempFileName(),
			CachePolicy = CachePolicy.None,
			StoragePerformance = StoragePerformance.CommitToDisk,
		};

		const int entries = 5000;

		using var tree = new BPlusTree<int, string>(options);

		for (var i = 0; i < entries; i++)
			tree[i] = Lipsum.Copy;
	}

	[Benchmark]
	public void Original_IntGuid()
	{
		var options = new PreForkCollection.BPlusTree<int, Guid>.OptionsV2(PreForkSerialization.PrimitiveSerializer.Int32, PreForkSerialization.PrimitiveSerializer.Guid)
		{
			CreateFile = PreForkCollection.CreatePolicy.Always,
			FileName = Path.GetTempFileName(),
			CachePolicy = PreForkCollection.CachePolicy.None,
			StoragePerformance = PreForkCollection.StoragePerformance.CommitToDisk,
		};

		const int entries = 5000;

		using var tree = new PreForkCollection.BPlusTree<int, Guid>(options);

		for (var i = 0; i < entries; i++)
			tree[i] = Guid.NewGuid();
	}

	[Benchmark]
	public void Fork_IntGuid()
	{
		var options = new BPlusTree<int, Guid>.Options(PrimitiveSerializer.Int32, PrimitiveSerializer.Guid)
		{
			CreateFile = CreatePolicy.Always,
			FileName = Path.GetTempFileName(),
			CachePolicy = CachePolicy.None,
			StoragePerformance = StoragePerformance.CommitToDisk,
		};

		const int entries = 5000;

		using var tree = new BPlusTree<int, Guid>(options);

		for (var i = 0; i < entries; i++)
			tree[i] = Guid.NewGuid();
	}

	[Benchmark]
	public void Original_IntInt()
	{
		var options = new PreForkCollection.BPlusTree<int, int>.OptionsV2(PreForkSerialization.PrimitiveSerializer.Int32, PreForkSerialization.PrimitiveSerializer.Int32)
		{
			CreateFile = PreForkCollection.CreatePolicy.Always,
			FileName = Path.GetTempFileName(),
			CachePolicy = PreForkCollection.CachePolicy.None,
			StoragePerformance = PreForkCollection.StoragePerformance.CommitToDisk,
		};

		const int entries = 5000;

		using var tree = new PreForkCollection.BPlusTree<int, int>(options);

		for (var i = 0; i < entries; i++)
			tree[i] = i;
	}

	[Benchmark]
	public void Fork_IntInt()
	{
		var options = new BPlusTree<int, int>.Options(PrimitiveSerializer.Int32, PrimitiveSerializer.Int32)
		{
			CreateFile = CreatePolicy.Always,
			FileName = Path.GetTempFileName(),
			CachePolicy = CachePolicy.None,
			StoragePerformance = StoragePerformance.CommitToDisk,
		};

		const int entries = 5000;

		using var tree = new BPlusTree<int, int>(options);

		for (var i = 0; i < entries; i++)
			tree[i] = i;
	}
}

public static class Lipsum
{
	public const string Copy = @"
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam aliquet non ipsum a euismod. Nam scelerisque, dolor quis sagittis pulvinar, eros tellus volutpat magna, at interdum enim massa sit amet quam. In tellus ipsum, rutrum eu lectus non, dapibus venenatis ipsum. Praesent a purus lacinia, congue ex quis, tempor diam. Proin sed ullamcorper dolor, vitae mollis augue. Duis sit amet augue vel augue molestie ultricies. Curabitur sed erat eu elit mollis semper sed nec lorem. Nullam vitae tellus ut ipsum euismod viverra. In quam nunc, finibus vitae massa ut, consectetur pharetra urna. Suspendisse accumsan, elit non blandit sagittis, libero magna consectetur est, eu mattis diam purus vel ante. Sed dapibus lorem eget fermentum varius. Vivamus gravida maximus sapien pulvinar aliquet. Aliquam porta purus ac velit lacinia fermentum. Etiam nec justo sit amet magna faucibus ullamcorper. Etiam eleifend consequat laoreet.
Nam ultrices, diam pellentesque iaculis scelerisque, nibh urna hendrerit risus, non iaculis ante elit id ante. Nam consequat arcu ut turpis consequat, ut tristique nibh viverra. Ut magna lorem, consequat in interdum elementum, consequat non felis. Phasellus a nisi vitae arcu tincidunt consequat nec eget libero. Cras lacus nisl, viverra ut diam mattis, euismod auctor odio. Curabitur tempor quis tellus ut elementum. Aenean eget neque ac lacus pharetra varius sit amet sagittis orci. Suspendisse laoreet lacinia mauris et consequat. Suspendisse blandit placerat eros, eget consectetur lectus egestas sed. Integer in consectetur tellus, ut tincidunt orci. Curabitur elit neque, condimentum sed scelerisque vitae, bibendum sit amet enim.
Vivamus dapibus volutpat elit, ut porttitor enim pulvinar ut. Nam eu felis mattis elit lacinia vestibulum eu in neque. Suspendisse porta eleifend mi. Morbi consequat ipsum vitae leo efficitur blandit. Praesent pulvinar ac lectus sit amet eleifend. Proin non turpis lorem. Quisque eu lectus ac nisi malesuada tempor et ac urna. Nulla nisi est, cursus non ipsum ac, hendrerit consequat eros.
Nullam vel augue pellentesque, sollicitudin lacus a, fermentum dui. Etiam pulvinar elit ipsum, non sollicitudin mauris mattis ut. Duis laoreet felis vel nisl imperdiet, at rutrum diam consequat. Proin sodales tempus sem non varius. Curabitur eget lectus tristique, pulvinar augue vitae, dapibus sem. Integer a accumsan urna. Ut blandit massa arcu, non ornare enim laoreet eu. Duis commodo dolor non elementum semper. Nam cursus dapibus velit, vitae malesuada odio tempor non. Sed a blandit lectus, id ullamcorper odio. Mauris eleifend, neque non posuere consequat, ipsum dolor sagittis felis, vitae pharetra orci dolor a tellus. Quisque dictum, eros in consectetur tristique, risus mauris ultrices lectus, id consequat tellus dolor in nibh. Nullam at maximus tortor. Morbi consectetur nibh neque, consectetur bibendum massa facilisis id. In hac habitasse platea dictumst.
In et euismod sem. Morbi elit turpis, maximus vel egestas ac, suscipit vitae nibh. Suspendisse odio tellus, bibendum faucibus tincidunt ut, cursus vel lacus. Donec massa diam, congue ut est eget, venenatis laoreet dolor. Sed neque magna, bibendum sit amet lacus nec, volutpat scelerisque augue. Interdum et malesuada fames ac ante ipsum primis in faucibus. Fusce lobortis consequat erat id convallis. ";
}
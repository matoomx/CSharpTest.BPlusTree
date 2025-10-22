using System.Collections.Generic;
using System.Buffers;

namespace CSharpTest.Collections.Generic;

public partial class OrderedEnumeration<T>
{
	private sealed partial class DeserializeStream
	{
		private sealed class BlockSequence : ReadOnlySequenceSegment<byte>
		{
			private readonly byte[] _block;
			public BlockSequence(byte[] block)
			{
				_block = block;
				Memory = block;
			}

			public BlockSequence Append(byte[] block)
			{
				var sequence = new BlockSequence(block)
				{
					RunningIndex = RunningIndex + Memory.Length
				};

				Next = sequence;
				return sequence;
			}

			public void ReIndex()
			{
				long runningIndex = 0;
				BlockSequence seq = this;

				while (seq != null)
				{
					seq.RunningIndex = runningIndex;
					runningIndex += seq.DataBlock.Length;
					seq = (BlockSequence)seq.Next;
				}
			}

			public BlockSequence LastSequence
			{
				get
				{
					BlockSequence seq = this;

					while (seq != null)
					{
						if (seq.Next == null)
							return seq;

						seq = (BlockSequence)seq.Next;
					}

					return null;
				}
			}

			public byte[] DataBlock => _block;

			public IEnumerable<BlockSequence> Sequences
			{
				get
				{
					BlockSequence seq = this;

					while (seq != null)
					{
						yield return seq;
						seq = (BlockSequence)seq.Next;
					}
				}
			}

			public IEnumerable<byte[]> DataBlocks
			{
				get
				{
					BlockSequence seq = this;

					while (seq != null)
					{
						yield return seq._block;
						seq = (BlockSequence)seq.Next;
					}
				}
			}
		}
	}
}

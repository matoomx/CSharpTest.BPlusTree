#region Copyright 2011-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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

namespace CSharpTest.Collections.Generic;

public sealed partial class TransactedCompoundFile
{
	ref struct BlockRef
    {
        public readonly uint Identity;
        public readonly int Section;
        public readonly int Offset;
        public readonly int Count;
        public int ActualBlocks;

        public BlockRef(uint block, int blockSize)
        {
            Identity = block;
            ActualBlocks = Count = (int)(block >> 28 & 0x0F) + 1;
            block &= 0x0FFFFFFF;
            int blocksPerSection = (blockSize >> 2);
            Section = (int)block / blocksPerSection;
            Offset = (int)block % blocksPerSection;

            if (Section < 0 || Section >= 0x10000000 || Offset <= 0 || (Offset + Count - 1) >= blocksPerSection - 1)
                throw new ArgumentOutOfRangeException(nameof(block));
        }

        public BlockRef(uint blockId, int blockSize, int actualBlocks)
            : this(blockId, blockSize)
        {
            ActualBlocks = actualBlocks;
        }
    }
}

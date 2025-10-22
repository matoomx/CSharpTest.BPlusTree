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
namespace CSharpTest.Collections.Generic;

public partial class ObjectKeepAlive
{
	[System.Diagnostics.DebuggerDisplay("{Start}-{Last}")]
    sealed class Entry
    {
        public readonly long Start;
        public readonly object[] Items;
        public long Last;
        public long Age;
        public int OffsetClear;
        public Entry Next;

        public Entry(long start)
        {
            Start = Last = start;
            OffsetClear = 0;
            Age = long.MaxValue;
            Items = new object[BucketSize];
            Next = null;
        }
    }
}

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
using System.Diagnostics;
using System.Threading;

namespace CSharpTest.Collections.Generic;

partial class BTreeFileStore
{
	[DebuggerDisplay("{Id}")]
    struct FileId : IStorageHandle
    {
        private static int _uniqueCounter = new Random().Next();
        public readonly uint Id;
        public readonly uint Unique;
        public FileId(uint id) : this(id, unchecked((uint)Interlocked.Increment(ref _uniqueCounter))) 
        { }
        public FileId(uint id, uint unique) 
        { 
            Id = id; 
            Unique = unique; 
        }

        bool IEquatable<IStorageHandle>.Equals(IStorageHandle other)
        { 
            return Equals(other); 
        }

        public override bool Equals(object other)
        {
            if (other is not FileId fid) return false;

            return Id.Equals(fid.Id) && Unique.Equals(fid.Unique);
        }
        public override int GetHashCode()
        { 
            return (int)(Id ^ Unique); 
        }
    }
}

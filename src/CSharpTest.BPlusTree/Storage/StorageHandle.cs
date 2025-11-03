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
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

[DebuggerDisplay("{Id}")]
[StructLayout(LayoutKind.Auto)]
public struct StorageHandle : IEquatable<StorageHandle>
{
    private static int _uniqueCounter = new Random().Next();
    public readonly uint Id;
    public readonly uint Unique;
	public StorageHandle(uint id) : this(id, unchecked((uint)Interlocked.Increment(ref _uniqueCounter))) 
    { }
	public StorageHandle(uint id, uint unique) 
    { 
        Id = id; 
        Unique = unique; 
    }

    public bool Equals(StorageHandle other)
    { 
        return Id == other.Id && Unique == other.Unique; 
    }

    public override bool Equals(object other)
    {
		return other is StorageHandle otherHandle && Equals(otherHandle);

	}
	public override int GetHashCode()
    { 
        return HashCode.Combine(Id, Unique); 
    }

	public static bool operator ==(StorageHandle left, StorageHandle right)
	{
		return left.Equals(right);
	}
	public static bool operator !=(StorageHandle left, StorageHandle right)
	{
		return !(left == right);
	}
}

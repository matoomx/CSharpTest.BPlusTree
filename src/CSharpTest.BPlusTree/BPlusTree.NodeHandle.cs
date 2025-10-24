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

public sealed class NodeHandle : IEquatable<NodeHandle>
{
    private readonly StorageHandle _storeHandle;
    private object _cacheEntry;

    public NodeHandle(StorageHandle storeHandle)
    {
        _storeHandle = storeHandle;
    }

    public StorageHandle StoreHandle { get { return _storeHandle; } }

    public bool TryGetCache<T>(out T cacheEntry) where T : class
    {
        cacheEntry = _cacheEntry as T;
        return cacheEntry != null;
    }

    public void SetCacheEntry(object cacheEntry)
    { 
		_cacheEntry = cacheEntry; 
	}

    public bool Equals(NodeHandle other) 
	{ 
		return _storeHandle.Equals(other._storeHandle); 
	}

	/// <summary> Returns true if the other object is equal to this one </summary>
	public sealed override bool Equals(object obj)
	{
		return obj is NodeHandle nh && Equals(nh);
	}

	/// <summary> Extracts the correct hash code </summary>
	public sealed override int GetHashCode()
	{
		return _storeHandle.GetHashCode();
	}
}
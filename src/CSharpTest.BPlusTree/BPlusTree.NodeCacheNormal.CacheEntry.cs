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

partial class BPlusTree<TKey, TValue>
{
    sealed partial class NodeCacheNormal
    {
        [System.Diagnostics.DebuggerDisplay("{Handle} = {Node}")]
        sealed class CacheEntry
        {
            private NodeCacheNormal _owner;
            public CacheEntry(NodeCacheNormal owner, NodeHandle handle)
            {
                Lock = owner.LockFactory.Create();
                Handle = handle;
                _owner = owner;
            }
            ~CacheEntry()
            {
                Lock.Dispose();
                if (!_owner._disposed)
                {
                    try
                    {
                        using (_owner._cacheLock.Write(_owner.Options.LockTimeout))
                        {
                            if (_owner._cache.TryGetValue(Handle, out WeakReference<CacheEntry> me) && me.IsAlive == false)
                                _owner._cache.Remove(Handle);
                        }
                    }
                    catch (ObjectDisposedException)
                    { }
                }
                Node = null;
                _owner = null;
            }

            public readonly ILockStrategy Lock;
            public readonly NodeHandle Handle;
            public Node Node;
        }
    }
}

#region Copyright 2012-2014 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{
    class StorageCache : INodeStorage, ITransactable
    {
        private readonly INodeStorage _store;
        private readonly LurchTable<StorageHandle, object> _cache, _dirty;
        private readonly Lock _flushSync;
        private readonly Action _writeBehindFunc;
        private bool? _asyncWriteBehind;
        private readonly int _asyncThreshold;

        ISerializer<Node> _serializer;

        public StorageCache(INodeStorage store, int sizeLimit)
        {
            _flushSync = new Lock();
            _asyncThreshold = 50;
            _writeBehindFunc = Flush;
            _asyncWriteBehind = null;

            _store = store;
            _cache = new LurchTable<StorageHandle, object>(LurchTableOrder.Access, sizeLimit, 1000000, sizeLimit >> 4, 1000, EqualityComparer<StorageHandle>.Default);
            _dirty = new LurchTable<StorageHandle, object>(LurchTableOrder.Modified, sizeLimit, 1000000, sizeLimit >> 4, 1000, EqualityComparer<StorageHandle>.Default);
            _dirty.ItemRemoved += OnItemRemoved;
        }

        public void Dispose()
        {
            using(_cache)
            {
                lock (_flushSync) // disallow concurrent async flush
                {
                    _dirty.ItemRemoved -= OnItemRemoved;
                    ClearCache();
                    _store.Dispose();
                }
            }
        }

        // Must SYNC on lock (_flushSync)
        private void ClearCache()
        {
            _cache.Clear();
            _dirty.Clear();
        }

        public void Commit()
        {
            lock (_flushSync) // disallow concurrent async flush
            {
                Flush();

				if (_store is ITransactable tstore)		
					tstore.Commit();
            }
        }

        public void Rollback()
        {
			if (_store is ITransactable tstore)
			{
				lock (_flushSync) // disallow concurrent async flush
				{
					_serializer = null;
					ClearCache();
					tstore.Rollback();
				}
			}
		}

        public void Reset()
        {
            lock (_flushSync) // disallow concurrent async flush
            {
                _serializer = null;
                ClearCache();
                _store.Reset();
            }
        }

        public StorageHandle OpenRoot(out bool isNew)
        {
            return _store.OpenRoot(out isNew);
        }

        public StorageHandle Create()
        {
            return _store.Create();
        }

        struct FetchFromStore<TNode> : ICreateOrUpdateValue<StorageHandle, object>
        {
            public INodeStorage Storage;
            public ISerializer<TNode> Serializer;
            public LurchTable<StorageHandle, object> DirtyCache;
            public TNode Value;
            public bool Success;

            public bool CreateValue(StorageHandle key, out object value)
            {
                if (DirtyCache.TryGetValue(key, out value) && value != null)
                {
                    Success = true;
                    Value = (TNode)value;
                    return true;
                }

                Success = Storage.TryGetNode(key, out Value, Serializer);
                if (Success)
                {
                    value = Value;
                    return true;
                }

                value = null;
                return false;
            }
            public bool UpdateValue(StorageHandle key, ref object value)
            {
                Success = value != null;
                Value = (TNode)value;
                return false;
            }
        }

        public bool TryGetNode<TNode>(StorageHandle handle, out TNode tnode, ISerializer<TNode> serializer)
        {
            _serializer ??= (ISerializer<Node>) serializer;

            var fetch = new FetchFromStore<TNode>
            {
                DirtyCache = _dirty,
                Storage = _store, 
                Serializer = serializer,
            };

            _cache.AddOrUpdate(handle, ref fetch);
            if (fetch.Success)
            {
                tnode = fetch.Value;
                return true;
            }
            tnode = default;
            return false;
        }

        public void Update<TNode>(StorageHandle handle, ISerializer<TNode> serializer, TNode tnode)
        {
            _serializer ??= (ISerializer<Node>)serializer;

            _cache[handle] = tnode;
            _dirty[handle] = tnode;

            var completion = _asyncWriteBehind;
            if (_dirty.Count > _asyncThreshold && completion is true)
            {
                bool locked = _flushSync.TryEnter();
                try
                {
                    if (locked)
                    {
                        completion = _asyncWriteBehind;
                        if (completion is true)
                        {
                            _writeBehindFunc();
                            _asyncWriteBehind = true;
                        }
                    }
                }
                finally
                {
                    if (locked)
						_flushSync.Exit();
                }
            }
        }

        public void Destroy(StorageHandle handle)
        {
            _dirty[handle] = null;
            _dirty.Remove(handle);
            _cache.Remove(handle);
            _store.Destroy(handle);
        }

        void OnItemRemoved(KeyValuePair<StorageHandle, object> item)
        {
            var ser = _serializer;
            if (ser != null && item.Value != null)
                _store.Update(item.Key, ser, (Node)item.Value);
        }

        private void Flush()
        {
            lock (_flushSync) // disallow concurrent async flush
            {
                try
                {
					while (_dirty.TryDequeue(out KeyValuePair<StorageHandle, object> value))
					{
					}
				}
                catch (ObjectDisposedException)
                { }
                finally
                {
                    _asyncWriteBehind = null;
                }
            }
        }

        StorageHandle ISerializer<StorageHandle>.ReadFrom(ReadOnlySequence<byte> data, ref SequencePosition position)
        {
            return _store.ReadFrom(data, ref position);
        }

        void ISerializer<StorageHandle>.WriteTo(StorageHandle value, IBufferWriter<byte> writer)
        {
            _store.WriteTo(value, writer);
        }
    }
}

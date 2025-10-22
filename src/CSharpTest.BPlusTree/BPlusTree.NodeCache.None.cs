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
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{
    /// <summary> performs a perfect cache of the entire tree </summary>
    sealed class NodeCacheNone : NodeCacheBase
    {
        readonly Dictionary<IStorageHandle, ILockStrategy> _list;
        readonly ReaderWriterLocking _lock;
        NodeHandle _root;

        public NodeCacheNone(Options options): base(Fix(options))
        {
            _lock = new ReaderWriterLocking();
            _list = [];
        }

        static Options Fix(Options options)
        {
            return options;
        }

        protected override void LoadStorage()
        {
			_root = new NodeHandle(Storage.OpenRoot(out bool isNew));
			_root.SetCacheEntry(LockFactory.Create());
            
            if (isNew)
                CreateRoot(_root);

			if (Storage.TryGetNode(_root.StoreHandle, out Node rootNode, NodeSerializer))
				_root.SetCacheEntry(LockFactory.Create());

			Check.Assert(rootNode != null, "Unable to load storage root.");
        }

        protected override NodeHandle RootHandle
        {
            get { return _root; }
        }

        public override void ResetCache()
        {
            _list.Clear();
        }

        public override void UpdateNode(NodePin node)
        {
            if (node.IsDeleted)
                using (_lock.Write(base.Options.LockTimeout))
                    _list.Remove(node.Handle.StoreHandle);
        }

        public override ILockStrategy CreateLock(NodeHandle handle, out object refobj)
        {
            ILockStrategy lck;
            using (_lock.Write(base.Options.LockTimeout))
            {
                if (!_list.TryGetValue(handle.StoreHandle, out lck))
                {
                    _list.Add(handle.StoreHandle, lck = LockFactory.Create());
                    handle.SetCacheEntry(lck);
                }
            }

            refobj = null;
            bool acquired = lck.TryWrite(base.Options.LockTimeout);
            Check.Assert<DeadlockException>(acquired);
            return lck;
        }

        protected override NodePin Lock(NodePin parent, LockType ltype, NodeHandle child)
        {
			if (!child.TryGetCache(out ILockStrategy lck))
			{
				using var wx = _lock.Write(base.Options.LockTimeout);
				
                if (!_list.TryGetValue(child.StoreHandle, out lck))
				{
					_list.Add(child.StoreHandle, lck = LockFactory.Create());
					child.SetCacheEntry(lck);
				}
			}

			bool success = ltype == LockType.Read ? lck.TryRead(base.Options.LockTimeout) : lck.TryWrite(base.Options.LockTimeout);
            
            Check.Assert<DeadlockException>(success);
            try
            {
				success = Storage.TryGetNode(child.StoreHandle, out Node node, NodeSerializer);
				Check.Assert<InvalidNodeHandleException>(success && node != null);

                return new NodePin(child, lck, ltype, ltype, lck, node, null);
            }
            catch
            {
                if (ltype == LockType.Read)
                    lck.ReleaseRead();
                else
                    lck.ReleaseWrite();
                
                throw;
            }
        }
    }
}

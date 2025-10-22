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

partial class BPlusTree<TKey, TValue>
{
    /// <summary> performs a perfect cache of the entire tree </summary>
    sealed partial class NodeCacheFull : NodeCacheBase
    {
        NodeHandle _root;
        public NodeCacheFull(Options options) : base(options)
        {}

        protected override void LoadStorage()
        {
			_root = new NodeHandle(Storage.OpenRoot(out bool isNew));
			_root.SetCacheEntry(new NodeWithLock(null, LockFactory.Create()));
            
            if (isNew)
                CreateRoot(_root);

			if (Storage.TryGetNode(_root.StoreHandle, out Node rootNode, NodeSerializer))
				_root.SetCacheEntry(new NodeWithLock(rootNode, LockFactory.Create()));

			Check.Assert(rootNode != null, "Unable to load storage root.");
        }

        protected override NodeHandle RootHandle
        {
            get { return _root; }
        }

        public override void ResetCache()
        {
			if (_root.TryGetCache(out NodeWithLock nlck))
				nlck.Node = null;
		}

        public override void UpdateNode(NodePin node)
        {
            if (!node.IsDeleted)
            {
				if (!node.Handle.TryGetCache(out NodeWithLock nlck))
					throw new InvalidNodeHandleException();
				
                nlck.Node = node.Ptr;
            }
        }

        public override ILockStrategy CreateLock(NodeHandle handle, out object refobj)
        {
            var nlck = new NodeWithLock(null, LockFactory.Create());
            handle.SetCacheEntry(nlck);
            refobj = nlck;
            bool acquired = nlck.Lock.TryWrite(base.Options.LockTimeout);
            Check.Assert<DeadlockException>(acquired);
            return nlck.Lock;
        }

        protected override NodePin Lock(NodePin parent, LockType ltype, NodeHandle child)
        {
			if (!child.TryGetCache(out NodeWithLock nlck))
				child.SetCacheEntry(nlck = new NodeWithLock(null, LockFactory.Create()));

			bool acquired = ltype == LockType.Read ? nlck.Lock.TryRead(base.Options.LockTimeout) : nlck.Lock.TryWrite(base.Options.LockTimeout);
            
            Check.Assert<DeadlockException>(acquired);
            try
            {
                if (nlck.Node == null)
                {
                    using (new SafeLock<DeadlockException>(nlck, base.Options.LockTimeout))
                        Storage.TryGetNode(child.StoreHandle, out nlck.Node, NodeSerializer);
                }

                Check.Assert<InvalidNodeHandleException>(nlck.Node != null);
                return new NodePin(child, nlck.Lock, ltype, ltype, nlck, nlck.Node, null);
            }
            catch
            {
                if (ltype == LockType.Read)
                    nlck.Lock.ReleaseRead();
                else
                    nlck.Lock.ReleaseWrite();
                throw;
            }
        }
    }
}

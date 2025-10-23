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

public sealed partial class BPlusTree<TKey, TValue>
{
	struct RootLock : IDisposable
    {
        readonly BPlusTree<TKey, TValue> _tree;
        private readonly LockType _type;
        private bool _locked;
        private bool _exclusive;
        private NodeVersion _version;
        public readonly NodePin Pin;

        public RootLock(BPlusTree<TKey, TValue> tree, LockType type, bool exclusiveTreeAccess)
        {
            tree.NotDisposed();
            _tree = tree;
            _type = type;
            _version = type == LockType.Read ? tree._storage.CurrentVersion : null;
            _exclusive = exclusiveTreeAccess;
            _locked = _exclusive ? _tree._selfLock.TryWrite(tree._options.LockTimeout) : _tree._selfLock.TryRead(tree._options.LockTimeout);
            Check.Assert<LockTimeoutException>(_locked);
            try
            {
                Pin = _tree._storage.LockRoot(type);
            }
            catch
            {
                if (_exclusive)
                    _tree._selfLock.ReleaseWrite();
                else
                    _tree._selfLock.ReleaseRead();
                throw;
            }
        }
        void IDisposable.Dispose()
        {
            Pin.Dispose();

            if (_locked && _exclusive)
                _tree._selfLock.ReleaseWrite();
            else if (_locked && !_exclusive)
                _tree._selfLock.ReleaseRead();

            _locked = false;
			NodeCacheBase.ReturnVersion(ref _version);

            if (_type != LockType.Read)
                _tree.OnChanged();
        }
    }

	RootLock LockRoot(LockType ltype) 
    { 
        return new RootLock(this, ltype, false); 
    }

	RootLock LockRoot(LockType ltype, bool exclusive) 
    { 
        return new RootLock(this, ltype, exclusive); 
    }
}
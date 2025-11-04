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

using System.Threading;
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

public sealed partial class LurchTable<TKey, TValue>
{
	[StructLayout(LayoutKind.Auto)]
	struct EnumState
    {
        private object _locked;
        public int Bucket, Current, Next;
        public void Init()
        {
            Bucket = -1;
            Current = 0;
            Next = 0;
            _locked = null;
        }

        public void Unlock()
        {
            if (_locked != null)
            {
                Monitor.Exit(_locked);
                _locked = null;
            }
        }

        public void Lock(object lck)
        {
            if (_locked != null)
                Monitor.Exit(_locked);
            Monitor.Enter(_locked = lck);
        }
    }
}

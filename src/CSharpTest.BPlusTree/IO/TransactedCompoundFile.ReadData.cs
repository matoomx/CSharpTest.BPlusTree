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
using System.Buffers;

namespace CSharpTest.Collections.Generic;

public sealed partial class TransactedCompoundFile
{
	public ref struct ReadData : IDisposable
    {
        private byte[] _rented;
        private readonly ArrayPool<byte> _pool;
		private ReadOnlySequence<byte> _data;
        public ReadData(byte[] rented, ArrayPool<byte> pool, int start = 0)
        {
			_rented = rented;
			_pool = pool;
			_data = new ReadOnlySequence<byte>(rented, start, rented.Length);
		}

		public ReadData(byte[] rented, ArrayPool<byte> pool, int start, int length)
		{
			_rented = rented;
			_pool = pool;
			_data = new ReadOnlySequence<byte>(rented, start, length);
		}

        public ReadOnlySequence<byte> Data => _data;


		public void Dispose()
        {
			_pool?.Return(_rented);
            _rented = null;
			_data = default;
		}

        public static ReadData Empty => new ReadData([], null);
	}
}

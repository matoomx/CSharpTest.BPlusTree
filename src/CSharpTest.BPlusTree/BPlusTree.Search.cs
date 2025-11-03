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
using System.Runtime.InteropServices;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{
	[StructLayout(LayoutKind.Auto)]
	private struct UpdateInfo : IUpdateValue<TKey, TValue>
    {
        private bool _updated;
        private TValue _oldValue, _newValue;
        private KeyValueUpdate<TKey, TValue> _fnUpdate;
        public UpdateInfo(KeyValueUpdate<TKey, TValue> fnUpdate) : this()
        {
            _fnUpdate = fnUpdate;
        }
        public UpdateInfo(TValue newValue) : this()
        {
            _newValue = newValue;
        }
        public bool UpdateValue(TKey key, ref TValue value)
        {
            _updated = true;
            _oldValue = value;
            if (_fnUpdate != null)
                value = _fnUpdate(key, value);
            else
                value = _newValue;
            return !_valueComparer.Equals(value, _oldValue);
        }
        public bool Updated { get { return _updated; } }
    }

	[StructLayout(LayoutKind.Auto)]
	private struct UpdateIfValue : IUpdateValue<TKey, TValue>
    {
        private bool _updated;
        private TValue _comparisonValue, _newValue;
		public UpdateIfValue(TValue newValue, TValue comparisonValue)
			: this()
		{
			_newValue = newValue;
            _comparisonValue = comparisonValue;
        }

        public bool UpdateValue(TKey key, ref TValue value)
        {
            if(_valueComparer.Equals(value, _comparisonValue))
            {
                _updated = true;
                if(!_valueComparer.Equals(value, _newValue))
                {
                    value = _newValue;
                    return true;
                }
            }
            return false;
        }
        public bool Updated { get { return _updated; } }
    }

    private bool Seek(NodePin thisLock, TKey key, out NodePin pin, out int offset)
    {
        NodePin myPin = thisLock, nextPin = null;
        var find = new Element(key);
		try
        {
            while (myPin != null)
            {
                Node me = myPin.Ptr;

                bool isValueNode = me.IsLeaf;
				if (me.BinarySearch(_itemComparer, find, out int ordinal) && isValueNode)
				{
					pin = myPin;
					myPin = null;
					offset = ordinal;
					return true;
				}
				if (isValueNode)
                    break; // not found.

                nextPin = _storage.Lock(myPin, me[ordinal].ChildNode);
                myPin.Dispose();
                myPin = nextPin;
                nextPin = null;
            }
        }
        finally
        {
            myPin?.Dispose();
            nextPin?.Dispose(); 
        }

        pin = null;
        offset = -1;
        return false;
    }

    private bool Search(NodePin thisLock, TKey key, ref TValue value)
    {
		if (Seek(thisLock, key, out NodePin pin, out int offset))
			using (pin)
			{
				value = pin.Ptr[offset].Payload;
				return true;
			}
		return false;
    }

    private bool SeekToEdge(NodePin thisLock, bool first, out NodePin pin, out int offset)
    {
        NodePin myPin = thisLock, nextPin = null;
        try
        {
            while (myPin != null)
            {
                Node me = myPin.Ptr;
                int ordinal = first ? 0 : me.Count - 1;
                if (me.IsLeaf)
                {
                    if (ordinal < 0 || ordinal >= me.Count)
                        break;

                    pin = myPin;
                    myPin = null;
                    offset = ordinal;
                    return true;
                }

                nextPin = _storage.Lock(myPin, me[ordinal].ChildNode);
                myPin.Dispose();
                myPin = nextPin;
                nextPin = null;
            }
        }
        finally
        {
            myPin?.Dispose();
            nextPin?.Dispose();
        }

        pin = null;
        offset = -1;
        return false;
    }

    private bool TryGetEdge(NodePin thisLock, bool first, out KeyValuePair<TKey, TValue> item)
    {
		if (SeekToEdge(thisLock, first, out NodePin pin, out int offset))
		{
			using (pin)
			{
				item = new KeyValuePair<TKey, TValue>(
					pin.Ptr[offset].Key,
					pin.Ptr[offset].Payload);
				return true;
			}
		}
		item = default;
        return false;
    }

    private bool Update<T>(NodePin thisLock, TKey key, ref T value) where T : IUpdateValue<TKey, TValue>
    {
		if (Seek(thisLock, key, out NodePin pin, out int offset))
			using (pin)
			{
				TValue newValue = pin.Ptr[offset].Payload;
				if (value.UpdateValue(key, ref newValue))
				{
					using NodeTransaction trans = _storage.BeginTransaction();
					trans.BeginUpdate(pin);
					pin.Ptr.SetValue(offset, key, newValue, _keyComparer);
					trans.UpdateValue(key, newValue);
					trans.Commit();
					return true;
				}
			}
		return false;
    }

    private int CountValues(NodePin thisLock)
    {
        if (thisLock.Ptr.IsLeaf)
            return thisLock.Ptr.Count;

        int count = 0;
        for (int i = 0; i < thisLock.Ptr.Count; i++)
        {
			using NodePin child = _storage.Lock(thisLock, thisLock.Ptr[i].ChildNode);
			count += CountValues(child);
		}
        return count;
    }
}

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
using System.Collections.Generic;
using CSharpTest.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPlusTreeTests;

public abstract class TestDictionary<TDictionary, TFactory, TKey, TValue> : TestCollection<TDictionary, TFactory, KeyValuePair<TKey, TValue>>
    where TDictionary : IDictionary<TKey, TValue>, IDisposable
    where TFactory : IFactory<TDictionary>, new()
{
    [TestMethod]
    public void TestAddRemoveByKey()
    {
        var sample = GetSample();
		using var test = Factory.Create();

		foreach (KeyValuePair<TKey, TValue> kv in sample)
			test.Add(kv.Key, kv.Value);

		foreach (KeyValuePair<TKey, TValue> kv in sample)
			Assert.IsTrue(test.ContainsKey(kv.Key));

		foreach (KeyValuePair<TKey, TValue> kv in sample)
			Assert.IsTrue(test.TryGetValue(kv.Key, out TValue cmp) && kv.Value.Equals(cmp));

		foreach (KeyValuePair<TKey, TValue> kv in sample)
			Assert.IsTrue(test.Remove(kv.Key));
	}

    [TestMethod]
    public void TestKeys()
    {
		using var test = Factory.Create();
		var keys = new List<TKey>();

		foreach (KeyValuePair<TKey, TValue> kv in GetSample())
		{
			test[kv.Key] = kv.Value;
			keys.Add(kv.Key);
		}

		var cmp = new List<TKey>(test.Keys);

		Assert.HasCount(keys.Count, cmp);
		
		for (int i = 0; i < keys.Count; i++)
			Assert.IsTrue(test.ContainsKey(keys[i]));
	}

    [TestMethod]
    public void TestValues()
    {
		using var test = Factory.Create();
		var values = new List<TValue>();

		foreach (KeyValuePair<TKey, TValue> kv in GetSample())
		{
			test[kv.Key] = kv.Value;
			values.Add(kv.Value);
		}

		var cmp = new List<TValue>(test.Values);
		Assert.HasCount(values.Count, cmp);
	}
}
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
using System.IO;

namespace CSharpTest.Collections.Generic;

partial class BPlusTree<TKey, TValue>
{
    /// <summary>
    /// Directly enumerates the contents of BPlusTree from disk in read-only mode.
    /// </summary>
    /// <param name="options"> The options normally used to create the <see cref="BPlusTree{TKey, TValue}"/> instance </param>
    /// <returns> Yields the Key/Value pairs found in the file </returns>
    public static IEnumerable<KeyValuePair<TKey, TValue>> EnumerateFile(Options options)
    {
        options = options.Clone();
        options.CreateFile = CreatePolicy.Never;
        options.ReadOnly = true;

		using INodeStorage store = options.CreateStorage();
		StorageHandle hroot = store.OpenRoot(out bool isnew);
		if (isnew)
			yield break;

		NodeSerializer nodeReader = new NodeSerializer(options, new NodeHandleSerializer(store));
		if (isnew || !store.TryGetNode(hroot, out Node root, nodeReader))
			throw new InvalidDataException();

		Stack<KeyValuePair<Node, int>> todo = new Stack<KeyValuePair<Node, int>>();
		todo.Push(new KeyValuePair<Node, int>(root, 0));

		while (todo.Count > 0)
		{
			KeyValuePair<Node, int> cur = todo.Pop();
			if (cur.Value == cur.Key.Count)
				continue;

			todo.Push(new KeyValuePair<Node, int>(cur.Key, cur.Value + 1));

			if (!store.TryGetNode(cur.Key[cur.Value].ChildNode.StoreHandle, out Node child, nodeReader))
				throw new InvalidDataException();

			if (child.IsLeaf)
			{
				for (int ix = 0; ix < child.Count; ix++)
					yield return child[ix].ToKeyValuePair();
			}
			else
			{
				todo.Push(new KeyValuePair<Node, int>(child, 0));
			}
		}
	}

}

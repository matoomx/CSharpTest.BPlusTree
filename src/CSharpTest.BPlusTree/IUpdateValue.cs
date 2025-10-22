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

/// <summary>
/// An interface to provide conditional or custom update logic to a concurrent dictionary.
/// </summary>
public interface IUpdateValue<TKey, TValue>
{
	/// <summary>
	/// Called when the key was found within the dictionary to produce a modified value to update the item
	/// to. Return true to continue with the update, or false to prevent the key/value from being updated.
	/// </summary>
	bool UpdateValue(TKey key, ref TValue value);
}

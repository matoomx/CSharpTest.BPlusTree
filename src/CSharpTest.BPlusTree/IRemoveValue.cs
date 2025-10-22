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
/// An interface to provide conditional removal of an item from a concurrent dictionary.
/// </summary>
/// <remarks>
/// Generally implemented as a struct and passed by ref to save stack space and to retrieve the values
/// that where inserted or updated.
/// </remarks>
public interface IRemoveValue<TKey, TValue>
{
	/// <summary>
	/// Called when the dictionary is about to remove the key/value pair provided, return true to allow
	/// it's removal, or false to prevent it from being removed.
	/// </summary>
	bool RemoveValue(TKey key, TValue value);
}

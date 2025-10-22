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
/// Provides an interface for tracking a limited number of references to objects for use in a WeakReference
/// cache.
/// </summary>
public interface IObjectKeepAlive
{
    /// <summary>
    /// Clears the entire keep-alive cache
    /// </summary>
    void Clear();

    /// <summary>
    /// Can be called periodically by external threads to ensure cleanup instead of depending upon calls to Add()
    /// </summary>
    void Tick();

    /// <summary>
    /// Cleans up expired items and adds the object to the list of items to keep alive.
    /// </summary>
    void Add(object item);
}

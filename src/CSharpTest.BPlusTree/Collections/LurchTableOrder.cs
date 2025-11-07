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

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Defines if and how items added to a LurchTable are linked together, this defines
/// the value returned from Peek/Dequeue as the oldest entry of the specified operation.
/// </summary>
public enum LurchTableOrder
{
    /// <summary> No linking </summary>
    None,
    /// <summary> Linked in insertion order </summary>
    Insertion,
    /// <summary> Linked by most recently inserted or updated </summary>
    Modified,
    /// <summary> Linked by most recently inserted, updated, or fetched </summary>
    Access,
}

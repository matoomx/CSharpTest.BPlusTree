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

/// <summary> Defines the storage type to use </summary>
public enum StorageType
{
    /// <summary> Uses in-memory storage </summary>
    Memory,
    /// <summary> Uses a file to store data, (Set by setting the FileName property) </summary>
    Disk,
    /// <summary> Uses a custom data store, (Set by setting the StorageSystem property) </summary>
    Custom
}

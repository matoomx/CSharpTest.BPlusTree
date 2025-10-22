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

/// <summary> Determines if the file specified should be created </summary>
public enum CreatePolicy
{
    /// <summary> Does not create a new file </summary>
    Never,
    /// <summary> Creates a new file even if one already exists </summary>
    Always,
    /// <summary> Creates a new file only if it does not exist </summary>
    IfNeeded
}

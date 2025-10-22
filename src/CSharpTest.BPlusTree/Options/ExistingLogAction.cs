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
/// Defines the action to perform when opening a BPlusTree with an existing log file.
/// </summary>
public enum ExistingLogAction
{
    /// <summary> 
    /// Infers the default wether or not the data file was created.  For newly created data
    /// files (CreatePolicy = Always, or IfNeeded and the file is missing) the default will
    /// be Truncate.  When existing data files are opened the default will ReplayAndCommit.
    /// </summary>
    Default,
    /// <summary> Ignore the existing entries in the log </summary>
    Ignore,
    /// <summary> Replay the log entries uncommitted </summary>
    Replay,
    /// <summary> Replay the log entries and commit the changes to the store </summary>
    ReplayAndCommit,
    /// <summary> Ignore the existing entries and truncate the log </summary>
    Truncate,
}

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
/// Defines the levels of durability the store will try to achieve.  'Uncommitted changes' in the descriptions below
/// refers to all changes made to the tree since the last call to CommitChanges() on the BPlusTree class.
/// </summary>
public enum StoragePerformance
{
    /// <summary> (100k rps) Uncommitted changes will be lost, a crash durring commit may corrupt state. </summary>
    /// <remarks> 
    /// No changes are committed until a call to Commit is made, durring the commit a partial write may corrupt the store.
    /// </remarks>
    Fastest = 1,
    /// <summary> (30k rps) Uses a system-cached transaction log to recover uncommitted changes after a process crash. </summary>
    /// <remarks> Will not corrupt state; however, in a power outage or system failure it may loose some comitted records. </remarks>
    LogFileInCache = 2,
    /// <summary> (8k rps) Every write will commit changes to the storage file immediately into system cache </summary>
    /// <remarks> May corrupt state and/or loose data in the event of a power outage </remarks>
    CommitToCache = 3,
    /// <summary> (2k rps) Uses a cache-writethrough transaction log to recover uncommitted changes after a power outage or system crash. </summary>
    /// <remarks> Complies with ACID durability requirements, can be expensive to recover from the log. </remarks>
    LogFileNoCache = 4,
    /// <summary> (1k rps) Every write will commit changes to the storage file immediately bypassing system cache (Slowest/Safest) </summary>
    /// <remarks> Complies with ACID durability requirements </remarks>
    CommitToDisk = 5,
	/// <summary> Defaults to using a transaction log in system cache for best performance/durability. </summary>
	Default = LogFileInCache,
}

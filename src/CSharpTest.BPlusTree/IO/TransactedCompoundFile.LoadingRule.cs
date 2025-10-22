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

public sealed partial class TransactedCompoundFile
{
	/// <summary>
	/// Defines the loading rule to apply when using a transacted file that was interrupted
	/// durring the commit process.
	/// </summary>
	public enum LoadingRule
    {
        /// <summary>
        /// Load all from Primary if valid, else load all from Secondary.  If both fail,
        /// load either Primary or Secondary for each segment.  This is the normal option,
        /// use the other options only when recovering from a commit that was incomplete.
        /// </summary>
        Default,
        /// <summary>
        /// If you previously called Commit(Action,T) on a prior instance and the Action
        /// delegate *was* called, then setting this value will ensure that only the 
        /// primary state storage is loaded, thereby ensuring you load the 'previous'
        /// state.
        /// </summary>
        Primary,
        /// <summary>
        /// If you previously called Commit(Action,T) on a prior instance and the Action
        /// delegate was *not* called, then setting this value will ensure that only the 
        /// secondary state storage is loaded, thereby ensuring you load the 'previous'
        /// state.
        /// </summary>
        Secondary,
    }
}

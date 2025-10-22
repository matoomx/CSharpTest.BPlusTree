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
/// A value representing the state/identifer/object of a single transaction.  The field's
/// meaning is defined by the ITrasactionLog implementation and is otherwise treated as an
/// opaque token identifier of the transaction.
/// </summary>
public struct TransactionToken
{
	public SerializeStream Stream;
	/// <summary> Undefined </summary>
	public long Handle;
	/// <summary> Undefined </summary>
	public int State;
	/// <summary> Undefined </summary>
	public short OperationCount;
}

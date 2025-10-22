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
using System;
using System.Collections.Generic;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Represents a transaction log of writes to a dictionary.
/// </summary>
public interface ITransactionLog<TKey, TValue> : IDisposable
{
    /// <summary>
    /// Replay the entire log file to the provided dictionary interface
    /// </summary>
    void ReplayLog(IDictionary<TKey, TValue> target);
    /// <summary>
    /// Replay the log file from the position provided and output the new log position
    /// </summary>
    void ReplayLog(IDictionary<TKey, TValue> target, ref long position);
    /// <summary>
    /// Merges the contents of the log with an existing ordered key/value pair collection.
    /// </summary>
    IEnumerable<KeyValuePair<TKey, TValue>> MergeLog(
        IComparer<TKey> keyComparer, IEnumerable<KeyValuePair<TKey, TValue>> existing);
    /// <summary>
    /// Truncate the log and remove all existing entries
    /// </summary>
    void TruncateLog();

    /// <summary>
    /// Notifies the log that a transaction is begining and create a token for this
    /// transaction scope.
    /// </summary>
    TransactionToken BeginTransaction();

    /// <summary> The provided key/value pair was added in the provided transaction </summary>
    void AddValue(ref TransactionToken token, TKey key, TValue value);
    /// <summary> The provided key/value pair was updated in the provided transaction </summary>
    void UpdateValue(ref TransactionToken token, TKey key, TValue value);
    /// <summary> The provided key/value pair was removed in the provided transaction </summary>
    void RemoveValue(ref TransactionToken token, TKey key);

    /// <summary>
    /// Commits the provided transaction
    /// </summary>
    void CommitTransaction(ref TransactionToken token);
    /// <summary>
    /// Abandons the provided transaction
    /// </summary>
    void RollbackTransaction(ref TransactionToken token);
    /// <summary>
    /// Returns the filename being currently used for transaction logging
    /// </summary>
    string FileName { get; }
    /// <summary>
    /// Returns the current size of the log file in bytes
    /// </summary>
    long Size { get; }
}

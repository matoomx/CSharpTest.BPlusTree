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
/// Options for bulk insertion
/// </summary>
public sealed class BulkInsertOptions
{
    private bool _inputIsSorted;
    private bool _commitOnCompletion;
    private bool _replaceContents;
    private DuplicateHandling _duplicateHandling;

    /// <summary> Constructs with defaults: false/RaisesException </summary>
    public BulkInsertOptions()
    {
        _replaceContents = false;
        _commitOnCompletion = true;
        _inputIsSorted = false;
        _duplicateHandling = DuplicateHandling.RaisesException;
    }

    /// <summary> Gets or sets a value that controls input presorting </summary>
    public bool InputIsSorted
    {
        get { return _inputIsSorted; }
        set { _inputIsSorted = value; }
    }

    /// <summary> Gets or sets the handling for duplicate key collisions </summary>
    public DuplicateHandling DuplicateHandling
    {
        get { return _duplicateHandling; }
        set { _duplicateHandling = value; }
    }

    /// <summary> When true (default) BulkInsert will call CommitChanges() on successfull completion </summary>
    public bool CommitOnCompletion
    {
        get { return _commitOnCompletion; }
        set { _commitOnCompletion = value; }
    }

    /// <summary> When false merges the data with the existing contents, set to true to replace all content </summary>
    public bool ReplaceContents
    {
        get { return _replaceContents; }
        set { _replaceContents = value; }
    }
}

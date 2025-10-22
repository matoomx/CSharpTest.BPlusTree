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
using System.IO;

namespace CSharpTest.Collections.Generic;

/// <summary>
/// Options used to initialize a TransactionLog
/// </summary>
public sealed class TransactionLogOptions<TKey, TValue>
{
    private readonly string _fileName;
    private readonly ISerializer<TKey> _keySerializer;
    private readonly ISerializer<TValue> _valueSerializer;
    private FileOptions _foptions;
    private int _fbuffer;
    private bool _readOnly;

    /// <summary>
    /// Options used to initialize a TransactionLog
    /// </summary>
    public TransactionLogOptions(string fileName, ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer)
    {
        _fileName = Check.NotEmpty(fileName);
        _keySerializer = Check.NotNull(keySerializer);
        _valueSerializer = Check.NotNull(valueSerializer);
        _foptions = FileOptions.WriteThrough;
        _fbuffer = 8;
    }

    /// <summary> The serializer for the TKey type </summary>
    public ISerializer<TKey> KeySerializer { get { return _keySerializer; } }
    /// <summary> The serializer for the TValue type </summary>
    public ISerializer<TValue> ValueSerializer { get { return _valueSerializer; } }

    /// <summary> The file name to read/write the log </summary>
    public string FileName { get { return _fileName; } }
    /// <summary> The file open options for appending to a log, default = WriteThrough </summary>
    public FileOptions FileOptions { get { return _foptions; } set { _foptions = value; } }
    /// <summary> The file buffer size, CAUTION: values above 16 bytes may leave data in memory </summary>
    public int FileBuffer { get { return _fbuffer; } set { _fbuffer = value; } }
    /// <summary> Gets or sets if the transaction log is treated as read-only </summary>
    public bool ReadOnly { get { return _readOnly; } set { _readOnly = value; } }

    /// <summary> Creates a shallow clone of the instance </summary>
    public TransactionLogOptions<TKey, TValue> Clone()
    {
        return (TransactionLogOptions<TKey, TValue>)MemberwiseClone();
    }
}

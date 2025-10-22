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
using System;
using System.IO;

namespace CSharpTest.Collections.Generic;

public sealed partial class TransactedCompoundFile
{
	/// <summary>
	/// Advanced Options used to construct a TransactedCompoundFile
	/// </summary>
	public class Options : ICloneable
    {
        private string _filePath;
        private int _blockSize;
        private FileOptions _fileOptions;
        private bool _createNew;
        private bool _commitOnWrite;
        private bool _commitOnDispose;
        private LoadingRule _loadingRule;
        private bool _readOnly;

        /// <summary>
        /// Constructs an Options instance
        /// </summary>
        /// <param name="filePath">The file name to use</param>
        public Options(string filePath)
        {
            _filePath = Check.NotNull(filePath);
            BlockSize = 4096;
            _fileOptions = FileOptions.None;
            _readOnly = false;
            _createNew = false;
            _commitOnWrite = false;
            _loadingRule = LoadingRule.Default;
        }
        /// <summary>
        /// Retrieves the file name that was provided to the constructor
        /// </summary>
        public string FilePath
        {
            get { return _filePath; }
        }
        /// <summary>
        /// Defines the block-size used for storing data.  Data storred in a given handle must be less than ((16*BlockSize)-8)
        /// </summary>
        public int BlockSize
        {
            get { return _blockSize; }
            set
            {
                int bit = 0;
                for (int i = value; i != 1; i >>= 1)
                    bit++;
                if (1 << bit != value)
                    throw new ArgumentException("BlockSize Must be a power of 2", "BlockSize");
                _blockSize = Check.InRange(value, 512, 65536);
            }
        }
        /// <summary>
        /// Returns the maximum number of bytes that can be written to a single handle base on the current BlockSize setting.
        /// </summary>
        public int MaxWriteSize
        {
            get { return (BlockSize*((BlockSize/4) - 2)) - BlockHeaderSize; }
        }
        /// <summary>
        /// The FileOptions used for writing to the file
        /// </summary>
        public FileOptions FileOptions
        {
            get { return _fileOptions; }
            set { _fileOptions = value; }
        }
        /// <summary>
        /// Gets or sets a flag that controls if the file is opened in read-only mode.  For ReadOnly
        /// files, another writer may exist; however, changes to the file will not be reflected until
        /// reload.
        /// </summary>
        public bool ReadOnly
        {
            get { return _readOnly; }
            set { _readOnly = value; }
        }
        /// <summary>
        /// True to create a new file, false to use the existing file.  If this value is false and the
        /// file does not exist an exception will be raised.
        /// </summary>
        public bool CreateNew
        {
            get { return _createNew; }
            set { _createNew = value; }
        }
        /// <summary>
        /// When true every write will rewrite the modified handle(s) back to disk, otherwise the
        /// handle state is kept in memory until a call to commit has been made.
        /// </summary>
        public bool CommitOnWrite
        {
            get { return _commitOnWrite; }
            set { _commitOnWrite = value; }
        }
        /// <summary>
        /// Automatically Commit the storage file when it's disposed.
        /// </summary>
        public bool CommitOnDispose
        {
            get { return _commitOnDispose; }
            set { _commitOnDispose = value; }
        }
        /// <summary>
        /// See comments on the LoadingRule enumerated type and Commit(Action,T)
        /// </summary>
        public LoadingRule LoadingRule
        {
            get { return _loadingRule; }
            set { _loadingRule = value; }
        }

        object ICloneable.Clone() { return Clone(); }
        /// <summary>
        /// Returns a copy of the options currently specified.
        /// </summary>
        public Options Clone()
        {
            return (Options)MemberwiseClone();
        }
    }
}

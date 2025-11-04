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
using System.Collections.Generic;
using System.IO;

namespace CSharpTest.Collections.Generic;

public partial class BPlusTree<TKey, TValue>
{
    /// <summary>
    /// Defines the options nessessary to construct a BPlusTree implementation
    /// </summary>
    public class Options : ICloneable
    {
        private string _logFileName;
        private readonly ISerializer<TKey> _keySerializer;
        private readonly ISerializer<TValue> _valueSerializer;

        private StorageType _storageType = StorageType.Memory;
        private CreatePolicy _createFile = CreatePolicy.Never;
        private bool _readOnly;
       
        private int _lockTimeout = 120000;
        private int _minimumChildNodes = 12;
        private int _maximumChildNodes = 32; // (assumes a key size of apx 100 bytes: (FileBlockSize - StoreageOverhead) / (AvgKeyBytes + ChildLinkSize)
        private int _fillChildNodes = 22;
        private int _minimumValueNodes = 3;
        private int _maximumValueNodes = 8; // (assumes a value size of apx 500 bytes: (FileBlockSize - StoreageOverhead) / (AvgValueBytes + AvgKeyBytes)
        private int _fillValueNodes = 4;
        private int _fileBlockSize = 4096;

        private string _fileName;
        private INodeStorage _storageSystem;

        private CachePolicy _cachePolicy = CachePolicy.Recent;
        private int _keepAliveMinHistory = 10;
        private int _keepAliveMaxHistory = 100;
        private int _keepAliveTimeout = 60000;

        /// <summary>
        /// Constructs the options configuration to initialize a BPlusTree instance
        /// </summary>
        public Options(ISerializer<TKey> keySerializer, ISerializer<TValue> valueSerializer, IComparer<TKey> comparer = null)
        {
            _keySerializer = Check.NotNull(keySerializer);
            _valueSerializer = Check.NotNull(valueSerializer);
            KeyComparer = comparer ?? ((typeof(TKey) == typeof(string)) ? (IComparer<TKey>)(IComparer<string>)AlternateComparers.StringOrdinal : Comparer<TKey>.Default);
        }

        /// <summary> Accesses the key serializer given to the constructor </summary>
        public ISerializer<TKey> KeySerializer { get { return _keySerializer; } }

        /// <summary> Accesses the key serializer given to the constructor </summary>
        public ISerializer<TValue> ValueSerializer { get { return _valueSerializer; } }

        /// <summary> Defines a custom IComparer&lt;T> to be used for comparing keys </summary>
        public IComparer<TKey> KeyComparer { get; set; } 

		/// <summary>
		/// Returns the DurabilityProtection of the underlying storage to create.
		/// </summary>
		public StoragePerformance StoragePerformance { get; set; } = StoragePerformance.Default;

		/// <summary>
		/// Defines the action to perform when opening a BPlusTree with an existing log file.
		/// </summary>
		public ExistingLogAction ExistingLogAction { get; set; } = ExistingLogAction.Default;

		/// <summary>
		/// Defines the number of bytes in the transaction log file before the BPlusTree will auto-commit
		/// and truncate the log.  Values equal to or less than zero will not auto-commit (default).
		/// </summary>
		public long TransactionLogLimit { get; set; } = -1;

		/// <summary>
		/// Calculates default node-threasholds based upon the average number of bytes in key and value
		/// </summary>
		public void CalcBTreeOrder(int avgKeySizeBytes, int avgValueSizeBytes)
        {
            CalculateOrder(avgKeySizeBytes, avgValueSizeBytes);
        }

        /// <summary>
        /// Calculates default node-threasholds based upon the average number of bytes in key and value
        /// </summary>
        public void CalculateOrder(int avgKeySizeBytes, int avgValueSizeBytes)
        {
            const int childLinkSize = 8;

            avgKeySizeBytes = Math.Max(0, Math.Min(ushort.MaxValue, avgKeySizeBytes));
            avgValueSizeBytes = Math.Max(0, Math.Min(ushort.MaxValue, avgValueSizeBytes));

            int maxChildNodes = Math.Min(256, Math.Max(4, FileBlockSize / (avgKeySizeBytes + childLinkSize)));
            int maxValueNodes = Math.Min(256, Math.Max(4, FileBlockSize / Math.Max(1, (avgValueSizeBytes + avgKeySizeBytes))));
            MaximumChildNodes = maxChildNodes;
            MinimumChildNodes = Math.Max(2, maxChildNodes / 3);
            MaximumValueNodes = maxValueNodes;
            MinimumValueNodes = Math.Max(2, maxValueNodes / 3);
        }
        /// <summary> 
        /// Can be used to explicitly specify the storage type, or by simply providing a file name this
        /// will be done for you.  If no file name was specified the default is to use a memory store.
        /// </summary>
        public StorageType StorageType
        {
            get { return _storageType; }
            set
            {
                InvalidConfigurationValueException.Assert(Enum.IsDefined(value), "StorageType", "The value is not defined.");
                InvalidConfigurationValueException.Assert(value != StorageType.Custom || _storageSystem != null, "StorageType", "Please provide the StorageSystem to be used.");
                InvalidConfigurationValueException.Assert(value != StorageType.Disk || _fileName != null, "StorageType", "Please provide the FileName to be used.");
                _storageType = value;
            }
        }
        /// <summary>
        /// Sets the BTree into a read-only mode (only supported when opening an existing file)
        /// </summary>
        public bool ReadOnly
        {
            get { return _readOnly; }
            set
            {
                if (value)
                {
                    InvalidConfigurationValueException.Assert(CreateFile == CreatePolicy.Never, "ReadOnly", "ReadOnly can only be used when CreateFile is Never");
                    InvalidConfigurationValueException.Assert(StorageType == StorageType.Disk, "ReadOnly", "ReadOnly can only be used with the file storage");
                    InvalidConfigurationValueException.Assert(File.Exists(FileName), "ReadOnly", "ReadOnly can only be used with an existing file");
                }
                _readOnly = value;
            }
        }

		/// <summary>
		/// Enable counting of the number of items in the tree.
		/// </summary>
		public bool EnableCount { get; set; }

        /// <summary>
        /// Commit transacions when tree is disposed.
        /// </summary>
        public bool CommitOnDispose { get; set; } = true;

		/// <summary>
		/// Sets the custom implementation of the storage back-end to use for the BTree
		/// </summary> 
		public INodeStorage StorageSystem
        {
            get { return _storageType == StorageType.Custom ? _storageSystem : null; }
            set { _storageSystem = Check.NotNull(value); _storageType = StorageType.Custom; }
        }
        /// <summary>
        /// Gets or sets the FileName that should be used to store the BTree
        /// </summary>
        public string FileName
        {
            get { return _fileName; }
            set
            {
                Check.NotNull(value);
                _fileName = value;
                _storageType = StorageType.Disk;
            }
        }
        /// <summary>
        /// Gets or sets the file-create policy used when backing with a file storage
        /// </summary>
        public CreatePolicy CreateFile
        {
            get { return _createFile; }
            set
            {
                InvalidConfigurationValueException.Assert(Enum.IsDefined(value), "CreateFile", "The value is not defined.");
                _createFile = value;
            }
        }
        /// <summary>
        /// Gets or sets the number of bytes per file-block used in the file storage
        /// </summary>
        public int FileBlockSize
        {
            get { return _fileBlockSize; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 512 && value <= 0x10000, "FileBlockSize", "The valid range is from 512 bytes to 64 kilobytes in powers of 2.");
                _fileBlockSize = value;
            }
        }
        /// <summary>
        /// Gets or sets the number of milliseconds to wait before failing a lock request, the default
        /// of two minutes should be more than adequate.
        /// </summary>
        public int LockTimeout
        {
            get { return _lockTimeout; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= -1 && value <= int.MaxValue, "LockTimeout", "The valid range is from -1 to MaxValue.");
                _lockTimeout = value;
            }
        }
        /// <summary>
        /// Gets or sets the locking factory to use for accessing shared data. The default is WriterOnlyLocking() 
        /// which does not perform read locks, rather it will rely on the cache of the btree and may preform dirty
        /// reads.  You can use any implementation of ILockFactory; however, the SimpleReadWriteLocking seems to 
        /// perform the most efficiently for both reader/writer locks.  Additionally wrapping that instance in a
        /// ReserveredWriterLocking() instance will allow reads to continue up until a writer begins the commit
        /// process.  If you are only accessing the BTree instance from a single thread this can be set to 
        /// IgnoreLocking. Be careful of using ReaderWriterLocking as the write-intesive nature of the BTree will 
        /// suffer extreme performance penalties with this lock.
        /// </summary>
        public ILockFactory LockingFactory { get; set; } = new LockFactory<WriterOnlyLocking>();
		/// <summary>
		/// Defines a reader/writer lock that used to control exclusive tree access when needed.  The public
		/// methods for EnableCount(), Clear(), and UnloadCache() each acquire an exclusive (write) lock while
		/// all other public methods acquire a shared (read) lock.  By default this lock is non-operational
		/// (an instance of IgnoreLocking) so if you need the above methods to work while multiple threads are
		/// accessing the tree, or if you exclusive access to the tree, specify a lock instance.  Since this
		/// lock is primarily a read-heavy lock consider using the ReaderWriterLocking or SimpleReadWriteLocking.
		/// </summary>
		public virtual ILockStrategy CallLevelLock { get; set; } = IgnoreLocking.Instance;

        /// <summary>
        /// A quick means of setting all the min/max values for the node counts using this value as a basis
        /// for the Maximum fields and one-quarter of this value for Minimum fields provided the result is in
        /// range.
        /// </summary>
        public int BTreeOrder
        {
            set
            {
                InvalidConfigurationValueException.Assert(value >= 4 && value <= 256, "BTreeOrder", "The valid range is from 4 to 256.");
                MaximumChildNodes = MaximumValueNodes = value;
                MinimumChildNodes = MinimumValueNodes = Math.Max(2, value >> 2);
            }
        }
        /// <summary>
        /// The smallest number of child nodes that should be linked to before refactoring the tree to remove
        /// this node.  In a 'normal' and/or purest B+Tree this is always half of max; however for performance
        /// reasons this B+Tree allow any value equal to or less than half of max but at least 2.
        /// </summary>
        /// <value>A number in the range of 2 to 128 that is at most half of MaximumChildNodes.</value>
        public int MinimumChildNodes
        {
            get { return _minimumChildNodes; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 2 && value <= (MaximumChildNodes / 2), "MinimumChildNodes", "The valid range is from 2 to (MaximumChildNodes / 2).");
                _minimumChildNodes = value;
                _fillChildNodes = ((_maximumChildNodes - _minimumChildNodes) >> 1) + _minimumChildNodes;
            }
        }
        /// <summary>
        /// The largest number of child nodes that should be linked to before refactoring the tree to split
        /// this node into two.  This property has a side-effect on MinimumChildNodes to ensure that it continues
        /// to be at most half of MaximumChildNodes.
        /// </summary>
        /// <value>A number in the range of 4 to 256.</value>
        public int MaximumChildNodes
        {
            get { return _maximumChildNodes; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 4 && value <= 256, "MaximumChildNodes", "The valid range is from 4 to 256.");
                _maximumChildNodes = value;
                _minimumChildNodes = Math.Min(value, _maximumChildNodes / 2);
                _fillChildNodes = ((_maximumChildNodes - _minimumChildNodes) >> 1) + _minimumChildNodes;
            }
        }
        /// <summary>
        /// The smallest number of values that should be contained in this node before refactoring the tree to remove
        /// this node.  In a 'normal' and/or purest B+Tree this is always half of max; however for performance
        /// reasons this B+Tree allow any value equal to or less than half of max but at least 2.
        /// </summary>
        /// <value>A number in the range of 2 to 128 that is at most half of MaximumValueNodes.</value>
        public int MinimumValueNodes
        {
            get { return _minimumValueNodes; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 2 && value <= (MaximumValueNodes / 2), "MinimumValueNodes", "The valid range is from 2 to (MaximumValueNodes / 2).");
                _minimumValueNodes = value;
                _fillValueNodes = ((_maximumValueNodes - _minimumValueNodes) >> 1) + _minimumValueNodes;
            }
        }
        /// <summary>
        /// The largest number of values that should be contained in this node before refactoring the tree to split
        /// this node into two.  This property has a side-effect on MinimumValueNodes to ensure that it continues
        /// to be at most half of MaximumValueNodes.
        /// </summary>
        /// <value>A number in the range of 4 to 256.</value>
        public int MaximumValueNodes
        {
            get { return _maximumValueNodes; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 4 && value <= 256, "MaximumValueNodes", "The valid range is from 4 to 256.");
                _maximumValueNodes = value;
                _minimumValueNodes = Math.Min(value, _maximumValueNodes / 2);
                _fillValueNodes = ((_maximumValueNodes - _minimumValueNodes) >> 1) + _minimumValueNodes;
            }
        }
        /// <summary>
        /// Determines how long loaded nodes stay in memory, Full keeps all loaded nodes alive and is the
        /// most efficient, The default Recent keeps recently visited nodes alive based on the CacheKeepAlive
        /// properties, and None does not cache the nodes at all but does maintain a cache of locks for 
        /// each node visited.
        /// </summary>
        public CachePolicy CachePolicy
        {
            get { return _cachePolicy; }
            set
            {
                InvalidConfigurationValueException.Assert(Enum.IsDefined(value), "CachePolicy", "The value is not defined.");
                _cachePolicy = value;
            }
        }


        /// <summary> 
        /// Determins minimum number of recently visited nodes to keep alive in memory.  This number defines
        /// the history size, not the number of distinct nodes.  This number will always be kept reguardless
        /// of the timeout.  Specify a value of 0 to allow the timeout to empty the cache.
        /// </summary>
        public int CacheKeepAliveMinimumHistory
        {
            get { return _keepAliveMinHistory; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 0 && value <= int.MaxValue, "CacheKeepAliveMinimumHistory", "The valid range is from 0 to MaxValue.");
                _keepAliveMinHistory = value;
                _keepAliveMaxHistory = Math.Max(_keepAliveMaxHistory, value);
            }
        }
        /// <summary> 
        /// Determins maximum number of recently visited nodes to keep alive in memory.  This number defines
        /// the history size, not the number of distinct nodes.  The ceiling is always respected reguardless
        /// of the timeout.  Specify a value of 0 to disable history keep alive.
        /// </summary>
        public int CacheKeepAliveMaximumHistory
        {
            get { return _keepAliveMaxHistory; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 0 && value <= int.MaxValue, "CacheKeepAliveMaximumHistory", "The valid range is from 0 to MaxValue.");
                _keepAliveMaxHistory = value;
                _keepAliveMinHistory = Math.Min(_keepAliveMinHistory, value);
            }
        }
        /// <summary>
        /// If the cache contains more that CacheKeepAliveMinimumHistory items, this timeout will start to
        /// remove those items until the cache history is reduced to CacheKeepAliveMinimumHistory.  It is 
        /// important to know that the BPlusTree itself contains no theads and this timeout will not be 
        /// respected if cache is not in use.
        /// </summary>
        public int CacheKeepAliveTimeout
        {
            get { return _keepAliveTimeout; }
            set
            {
                InvalidConfigurationValueException.Assert(value >= 0 && value <= int.MaxValue, "CacheKeepAliveTimeout", "The valid range is from 0 to MaxValue.");
                _keepAliveTimeout = value;
            }
        }

        /// <summary>
        /// Creates a shallow clone of the configuration options.
        /// </summary>
        public Options Clone() { return (Options)MemberwiseClone(); }
        object ICloneable.Clone() { return MemberwiseClone(); }

        /// <summary> Enables or disables the caching and reordering of node write operations </summary>
        protected void SetStorageCache(bool cached) { UseStorageCache = cached; }
        /// <summary> Sets the transaction log to use </summary>
        public void SetLogFile(ITransactionLog<TKey, TValue> logFile) { LogFile = logFile; }
		public ITransactionLog<TKey, TValue> GetLogFile() { return LogFile; }

		internal bool UseStorageCache;
        internal ITransactionLog<TKey, TValue> LogFile;

        /// <summary> Used to create the correct storage type </summary>
        internal INodeStorage CreateStorage()
        {
            if (StorageType == StorageType.Custom) return Check.NotNull(StorageSystem);
            if (StorageType == StorageType.Memory) return new BTreeMemoryStore();

            InvalidConfigurationValueException.Assert(StorageType == StorageType.Disk, "StorageType", "Unknown value defined.");
            bool exists = File.Exists(FileName);
            if (exists && new FileInfo(FileName).Length == 0)
            {
                exists = false;
                File.Delete(FileName);
            }
            bool createNew = CreateFile == CreatePolicy.Always || (exists == false && CreateFile == CreatePolicy.IfNeeded);

            if (!exists && !createNew)
                throw new InvalidConfigurationValueException("The file does not exist and CreateFile is Never");

            var foptions = new TransactedCompoundFile.Options(FileName)
            {
                BlockSize = FileBlockSize,
                FileOptions = FileOptions.None,
                ReadOnly = ReadOnly,
                CreateNew = createNew,
			};

            switch (StoragePerformance)
            {
                case StoragePerformance.Fastest:
                {
                    SetStorageCache(true);
                    break;
                }
                case StoragePerformance.CommitToCache:
                {
                    foptions.FileOptions = FileOptions.None;
                    foptions.CommitOnWrite = true;
                    break;
                }
                case StoragePerformance.CommitToDisk:
                {
                    foptions.FileOptions = FileOptions.WriteThrough;
                    foptions.CommitOnWrite = true;
                    break;
                }
				case StoragePerformance.LogFileInCache:
                case StoragePerformance.LogFileNoCache:
                {
                    SetStorageCache(true);
                    if (LogFile == null)
                    {
                        _logFileName ??= Path.ChangeExtension(FileName, ".tlog");
                        SetLogFile(new TransactionLog<TKey, TValue>( new TransactionLogOptions<TKey, TValue>(_logFileName, KeySerializer, ValueSerializer)
                        {
                            FileOptions = StoragePerformance == StoragePerformance.LogFileNoCache ? FileOptions.WriteThrough : FileOptions.None,                              
                        }));
                    }

                    foptions.CommitOnDispose = CommitOnDispose;

					break;
                }
                default:
                    throw new InvalidConfigurationValueException("The configuration option is not valid.");
            }

            return new BTreeFileStore(foptions);
        }
        /// <summary> The desired fill-size of node that contain children </summary>
        internal int FillChildNodes { get { return _fillChildNodes; } }
        /// <summary> The desired fill-size of node that contain values </summary>
        internal int FillValueNodes { get { return _fillValueNodes; } }
        /// <summary>
        /// Creates the keep-alive object reference tracking implementation
        /// </summary>
        internal ObjectKeepAlive CreateCacheKeepAlive()
        {
            return new ObjectKeepAlive(
                CacheKeepAliveMinimumHistory,
                CacheKeepAliveMaximumHistory,
                TimeSpan.FromMilliseconds(CacheKeepAliveTimeout));
        }
    }
}
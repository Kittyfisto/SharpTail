﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Threading;
using log4net;
using Metrolib;
using Tailviewer.BusinessLogic;
using Tailviewer.BusinessLogic.LogFiles;

namespace Tailviewer.Core.LogFiles
{
	/// <summary>
	///     Responsible for merging consecutive lines into multi-line log entries,
	///     if they belong together.
	/// </summary>
	/// <remarks>
	///     Two lines are defined to belong together if the first line contains a log
	///     level and the next one does not.
	/// </remarks>
	public sealed class MultiLineLogFile
		: AbstractLogFile
			, ILogFileListener
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const int MaximumBatchSize = 10000;

		private readonly List<LogEntryInfo> _indices;
		private readonly TimeSpan _maximumWaitTime;
		private readonly ConcurrentQueue<LogFileSection> _pendingModifications;
		private readonly ILogFile _source;
		private LogEntryInfo _currentLogEntry;
		private LogLineIndex _currentSourceIndex;
		private ErrorFlags _error;
		private Size _fileSize;

		private LogFileSection _fullSourceSection;
		private DateTime _lastModified;
		private int _maxCharactersPerLine;
		private DateTime? _startTimestamp;
		private LevelFlags _currentLogEntryLevel;

		private DateTime _created;
		//private readonly List<LogFileSection> _allModifications;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="taskScheduler"></param>
		/// <param name="source"></param>
		/// <param name="maximumWaitTime"></param>
		public MultiLineLogFile(ITaskScheduler taskScheduler, ILogFile source, TimeSpan maximumWaitTime)
			: base(taskScheduler)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			_maximumWaitTime = maximumWaitTime;
			_pendingModifications = new ConcurrentQueue<LogFileSection>();
			//_allModifications = new List<LogFileSection>();
			_indices = new List<LogEntryInfo>();
			_currentLogEntry = new LogEntryInfo(-1, 0);
			
			_source = source;
			_source.AddListener(this, maximumWaitTime, MaximumBatchSize);
			StartTask();
		}

		/// <inheritdoc />
		public override int MaxCharactersPerLine => _maxCharactersPerLine;

		/// <inheritdoc />
		public override ErrorFlags Error => _error;

		/// <inheritdoc />
		public override DateTime? StartTimestamp => _startTimestamp;

		/// <inheritdoc />
		public override DateTime LastModified => _lastModified;

		/// <inheritdoc />
		public override DateTime Created => _created;

		/// <inheritdoc />
		public override Size Size => _fileSize;

		/// <inheritdoc />
		public override int Count => (int) _currentSourceIndex;

		/// <inheritdoc />
		public void OnLogFileModified(ILogFile logFile, LogFileSection section)
		{
			_pendingModifications.Enqueue(section);
			ResetEndOfSourceReached();
		}

		/// <inheritdoc />
		protected override void DisposeAdditional()
		{
			_source.RemoveListener(this);
		}

		/// <inheritdoc />
		public override int OriginalCount => _source.OriginalCount;

		/// <inheritdoc />
		public override void GetColumn<T>(LogFileSection section, ILogFileColumn<T> column, T[] buffer, int destinationIndex)
		{
			_source.GetColumn(section, column, buffer);
		}

		/// <inheritdoc />
		public override void GetColumn<T>(IReadOnlyList<LogLineIndex> indices, ILogFileColumn<T> column, T[] buffer, int destinationIndex)
		{
			_source.GetColumn(indices, column, buffer);
		}

		/// <inheritdoc />
		public override void GetEntries(LogFileSection section, ILogEntries buffer, int destinationIndex)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public override void GetEntries(IReadOnlyList<LogLineIndex> indices, ILogEntries buffer, int destinationIndex)
		{
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public override void GetSection(LogFileSection section, LogLine[] dest)
		{
			_source.GetSection(section, dest);
			lock (_indices)
			{
				for (var i = 0; i < section.Count; ++i)
					dest[i] = PatchNoLock(dest[i]);
			}
		}

		/// <inheritdoc />
		public override LogLine GetLine(int index)
		{
			var actualLine = _source.GetLine(index);
			LogLine line;

			lock (_indices)
			{
				line = PatchNoLock(actualLine);
			}

			return line;
		}

		/// <inheritdoc />
		public override double Progress => 1;

		private LogLine PatchNoLock(LogLine line)
		{
			var info = _indices[line.LineIndex];

			LevelFlags level;
			DateTime? timestamp;
			if (line.LineIndex != info.FirstLineIndex)
			{
				// This line belongs to the previous line and together they form
				// (part of) a log entry. Even though only a single line mentions
				// the log level, all lines are given the same log level.
				var firstLine = _source.GetLine((int) info.FirstLineIndex);
				level = firstLine.Level;
				timestamp = firstLine.Timestamp;
			}
			else
			{
				level = line.Level;
				timestamp = line.Timestamp;
			}

			return new LogLine(line.LineIndex, info.EntryIndex,
				line.Message,
				level,
				timestamp);
		}

		/// <inheritdoc />
		protected override TimeSpan RunOnce(CancellationToken token)
		{
			var lastCount = _fullSourceSection.Count;
			bool performedWork = false;

			LogFileSection section;
			while (_pendingModifications.TryDequeue(out section) && !token.IsCancellationRequested)
			{
				if (section.IsReset)
				{
					Clear();
				}
				else if (section.IsInvalidate)
				{
					Invalidate(section);
				}
				else
				{
					_fullSourceSection = LogFileSection.MinimumBoundingLine(_fullSourceSection, section);
				}
				//_allModifications.Add(section);
				performedWork = true;
			}

			if (!_fullSourceSection.IsEndOfSection(_currentSourceIndex))
			{
				var remaining = Math.Min(_fullSourceSection.Count - _currentSourceIndex, MaximumBatchSize);
				var buffer = new LogLine[remaining];
				_source.GetSection(new LogFileSection(_currentSourceIndex, remaining), buffer);
				LogLineIndex? resetIndex = null;

				lock (_indices)
				{
					for (var i = 0; i < remaining; ++i)
					{
						var line = buffer[i];
						if (_currentLogEntry.EntryIndex.IsInvalid ||
						    line.Level != LevelFlags.None ||
						    _currentLogEntryLevel == LevelFlags.None)
						{
							_currentLogEntry = _currentLogEntry.NextEntry(line.LineIndex);
							_currentLogEntryLevel = line.Level;
						}
						else if (_currentLogEntry.FirstLineIndex < lastCount && resetIndex == null)
						{
							var index = _currentLogEntry.FirstLineIndex;
							resetIndex = index;

							_currentLogEntryLevel = _source.GetLine((int) index).Level;
						}
						_indices.Add(_currentLogEntry);
					}
				}

				if (resetIndex != null)
				{
					var resetCount = lastCount - resetIndex.Value;
					if (resetCount > 0)
						Listeners.Invalidate((int) resetIndex.Value, resetCount);
				}

				_currentSourceIndex += remaining;
			}

			_maxCharactersPerLine = _source.MaxCharactersPerLine;
			_error = _source.Error;
			_startTimestamp = _source.StartTimestamp;
			_lastModified = _source.LastModified;
			_created = _source.Created;
			_fileSize = _source.Size;

			if (_indices.Count != _currentSourceIndex)
			{
				Log.ErrorFormat("Inconsistency detected: We have {0} indices for {1} lines", _indices.Count,
					_currentSourceIndex);
			}
			
			Listeners.OnRead((int)_currentSourceIndex);

			if (_source.EndOfSourceReached && _fullSourceSection.IsEndOfSection(_currentSourceIndex))
			{
				SetEndOfSourceReached();
			}

			if (performedWork)
				return TimeSpan.Zero;

			return _maximumWaitTime;
		}

		private void Invalidate(LogFileSection section)
		{
			var firstInvalidIndex = LogLineIndex.Min(_fullSourceSection.LastIndex, section.Index);
			var lastInvalidIndex = LogLineIndex.Min(_fullSourceSection.LastIndex, section.LastIndex);
			var invalidateCount = lastInvalidIndex - firstInvalidIndex + 1;
			var previousSourceIndex = _currentSourceIndex;

			_fullSourceSection = new LogFileSection(0, (int)firstInvalidIndex);
			if (_fullSourceSection.Count > 0)
			{
				// It's possible (likely) that we've received an invalidation for a region of the source
				// that we've already processed (i.e. created indices for). If that's the case, then we need
				// to rewind the index. Otherwise nothing needs to be done...
				var newIndex = _fullSourceSection.LastIndex + 1;
				if (newIndex < _currentSourceIndex)
				{
					_currentSourceIndex = newIndex;
				}
			}
			else
			{
				_currentSourceIndex = 0;
			}

			lock (_indices)
			{
				var toRemove = _indices.Count - lastInvalidIndex;
				if (toRemove > 0)
				{
					_indices.RemoveRange((int)firstInvalidIndex, toRemove);
					_currentLogEntry = new LogEntryInfo(firstInvalidIndex - 1, 0);
				}
				if (previousSourceIndex != _currentSourceIndex)
				{
					_indices.RemoveRange((int) _currentSourceIndex, _indices.Count - _currentSourceIndex);
				}
			}

			if (_indices.Count != _currentSourceIndex)
			{
				Log.ErrorFormat("Inconsistency detected: We have {0} indices for {1} lines", _indices.Count,
					_currentSourceIndex);
			}

			Listeners.Invalidate((int)firstInvalidIndex, invalidateCount);
		}

		private void Clear()
		{
			_fullSourceSection = new LogFileSection(0, 0);
			_currentSourceIndex = 0;
			_currentLogEntry = new LogEntryInfo(-1, 0);
			lock (_indices)
			{
				_indices.Clear();
			}
			Listeners.OnRead(-1);
		}

		private struct LogEntryInfo
		{
			public readonly LogEntryIndex EntryIndex;
			public readonly LogLineIndex FirstLineIndex;

			public LogEntryInfo(LogEntryIndex entryIndex, LogLineIndex firstLineIndex)
			{
				EntryIndex = entryIndex;
				FirstLineIndex = firstLineIndex;
			}

			[Pure]
			public LogEntryInfo NextEntry(LogLineIndex lineLineIndex)
			{
				return new LogEntryInfo(EntryIndex + 1, lineLineIndex);
			}

			public override string ToString()
			{
				return string.Format("Log entry {0} starting at line {1}", EntryIndex, FirstLineIndex);
			}
		}
	}
}
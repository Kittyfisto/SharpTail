﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Tailviewer.BusinessLogic.LogFiles;

namespace Tailviewer.Core.LogFiles.Merged
{
	/// <summary>
	///     Responsible for storing the index structure of a <see cref="MergedLogFile" />.
	///     The index consists of a mapping between source and output lines (i.e. the n-th line
	///     of the merged log file is actually the m-th line of the o-th source).
	/// </summary>
	internal sealed class MergedLogFileIndex
	{
		private readonly List<MergedLogLineIndex> _indices;
		private readonly ILogFile[] _sources;
		private readonly object _syncRoot;

		public MergedLogFileIndex(params ILogFile[] sources)
		{
			_sources = sources;
			_indices = new List<MergedLogLineIndex>();
			_syncRoot = new object();
		}

		public int Count
		{
			get
			{
				lock (_syncRoot)
				{
					return _indices.Count;
				}
			}
		}

		[Pure]
		public IReadOnlyList<MergedLogLineIndex> Get(LogFileSection section)
		{
			var indices = new MergedLogLineIndex[section.Count];
			lock (_syncRoot)
			{
				if (section.Index.Value < _indices.Count)
				{
					var count = Math.Min((section.Index + section.Count).Value, _indices.Count);
					_indices.CopyTo(section.Index.Value, indices, 0, count);
					for (int i = 0; i < section.Count - count; ++i)
					{
						indices[section.LastIndex + i] = MergedLogLineIndex.Invalid;
					}
				}
				else
				{
					for (int i = 0; i < section.Count; ++i)
					{
						indices[i] = MergedLogLineIndex.Invalid;
					}
				}
			}

			return indices;
		}

		/// <summary>
		///     Processes several changes in one go.
		///     The more changes given in one go, the better the algorithm will perform overall.
		/// </summary>
		/// <param name="pendingModifications"></param>
		/// <returns></returns>
		public IEnumerable<LogFileSection> Process(params MergedLogFilePendingModification[] pendingModifications)
		{
			return Process((IEnumerable<MergedLogFilePendingModification>) pendingModifications);
		}

		/// <summary>
		///     Processes several changes in one go.
		///     The more changes given in one go, the better the algorithm will perform overall.
		/// </summary>
		/// <param name="pendingModifications"></param>
		/// <returns></returns>
		public IEnumerable<LogFileSection> Process(IEnumerable<MergedLogFilePendingModification> pendingModifications)
		{
			var entries = GetEntries(pendingModifications);
			return Process(entries);
		}

		private List<LogFileSection> Process(IReadOnlyList<MergedLogFileSection> pendingModifications)
		{
			// This method keeps track of the changes made
			// to the index structure in this object and then returns
			// the list once it is done.
			// It will try to compress the changes to the bare minimum
			// (which it can do if given more pending modifications at once).
			var changes = new List<LogFileSection>(capacity: 3);

			// TODO: Process invalidations
			// TODO: Process clears

			var indices = CreateIndices(pendingModifications);

			var invalidated = false;
			int invalidationIndex = -1;

			lock (_syncRoot)
			{
				var appendStartingIndex = _indices.Count;

				foreach (var index in indices)
				{
					var insertionIndex = FindInsertionIndexNoLock(index);
					if (insertionIndex < _indices.Count)
						if (!invalidated)
						{
							// Here's the awesome thing: We're inserting a "pre-sorted" list of indices
							// into our index buffer. Therefore, the very first index which requires an invalidation
							// is also the LOWEST POSSIBLE index which could require an invalidation and therefore
							// there can only ever be one invalidation while this method is executing.

							// We do need to take into account that even though we're invalidating a region,
							// that doesn't mean we also didn't append something previously....
							var appendCount = _indices.Count - appendStartingIndex;
							if (appendCount > 0)
								changes.Add(new LogFileSection(appendStartingIndex, appendCount));

							var invalidationCount = _indices.Count - insertionIndex;
							changes.Add(LogFileSection.Invalidate(insertionIndex, invalidationCount));

							// There's still possible append's after this invalidation...
							appendStartingIndex = insertionIndex;
							invalidationIndex = insertionIndex;
							invalidated = true;
						}

					_indices.Insert(insertionIndex, index);
				}

				var appendCount2 = _indices.Count - appendStartingIndex;
				if (appendCount2 > 0)
					changes.Add(new LogFileSection(appendStartingIndex, appendCount2));

				if (invalidated)
				{
					// TODO: Update indices from 'invalidationIndex' onward
					// The log entry index has possibly changed
				}
			}

			return changes;
		}

		[Pure]
		private int FindInsertionIndexNoLock(MergedLogLineIndex index)
		{
			// TODO: Find out if a binary search performs better
			for (var i = _indices.Count - 1; i >= 0; --i)
				if (index.Timestamp > _indices[i].Timestamp)
					return i + 1;

			return 0;
		}

		/// <summary>
		///     Retrieves the content the given modifications concern.
		///     Only retrieves the <see cref="LogFileColumns.Index" />, <see cref="LogFileColumns.LogEntryIndex" />
		///     and <see cref="LogFileColumns.Timestamp" /> columns (as these are the only ones required in order to
		///     merge stuff).
		/// </summary>
		/// <param name="pendingModifications"></param>
		/// <returns></returns>
		[Pure]
		private static IReadOnlyList<MergedLogFileSection> GetEntries(
			IEnumerable<MergedLogFilePendingModification> pendingModifications)
		{
			var columns = new ILogFileColumn[]
			{
				LogFileColumns.Index,
				LogFileColumns.LogEntryIndex,
				LogFileColumns.Timestamp
			};
			var sections = new List<MergedLogFileSection>();
			foreach (var pendingModification in pendingModifications)
			{
				var logFile = pendingModification.LogFile;
				var section = pendingModification.Section;
				if (!section.IsInvalidate &&
				    !section.IsReset)
				{
					var entries = logFile.GetEntries(section, columns);
					sections.Add(new MergedLogFileSection(logFile, section, entries));
				}
				else
				{
					sections.Add(new MergedLogFileSection(logFile, section));
				}
			}

			return sections;
		}

		/// <summary>
		///     Creates a sorted list of <see cref="MergedLogLineIndex" />s concerning the given modifications.
		/// </summary>
		/// <remarks>
		///     This method skips both <see cref="LogFileSection.IsReset" /> and <see cref="LogFileSection.IsInvalidate" />.
		/// </remarks>
		/// <param name="pendingModifications"></param>
		/// <returns></returns>
		[Pure]
		private static IReadOnlyList<MergedLogLineIndex> CreateIndices(
			IReadOnlyList<MergedLogFileSection> pendingModifications)
		{
			var indices = new List<MergedLogLineIndex>();
			foreach (var pendingModification in pendingModifications)
			{
				var entries = pendingModification.Entries;
				foreach (var entry in entries)
				{
					var index = entry.GetValue(LogFileColumns.Index);
					var entryIndex = entry.GetValue(LogFileColumns.LogEntryIndex);
					var timestamp = entry.GetValue(LogFileColumns.Timestamp);
					if (index.IsValid &&
					    entryIndex
						    .IsValid && //< Invalid values are possible if the file has been invalidated in between it sending us a change and us having retrieved the corresponding data
					    timestamp != null) //< Not every line has a timestamp
						indices.Add(new MergedLogLineIndex(index.Value, mergedLogEntryIndex: 0,
						                                   originalLogEntryIndex: entryIndex.Value, logFileIndex: 0,
						                                   timestamp: timestamp.Value));
				}
			}

			indices.Sort(new MergedLogLineIndexComparer());

			return indices;
		}
	}
}
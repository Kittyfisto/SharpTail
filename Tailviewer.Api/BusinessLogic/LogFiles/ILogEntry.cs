﻿using System;

namespace Tailviewer.BusinessLogic.LogFiles
{
	/// <summary>
	///     Provides read-only access to a log entry of a <see cref="ILogFile" />.
	/// </summary>
	/// <remarks>
	///     This interface is meant to replace <see cref="LogLine" />.
	///     With its introduction, <see cref="LogLineIndex" /> can be removed and be replaced
	///     with <see cref="LogEntryIndex" />.
	/// </remarks>
	public interface ILogEntry
	{
		/// <summary>
		///     The raw content of this log entry.
		/// </summary>
		/// <remarks>
		///     Might not be readable by a humand, depending on the data source.
		/// </remarks>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		string RawContent { get; }

		/// <summary>
		///     The index of this log entry, from 0 to the number of log entries.
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		LogEntryIndex Index { get; }

		/// <summary>
		///     The index of the log entry this one was created from.
		///     Only differs from <see cref="Index" /> when this log entry has been created
		///     through operations such as filtering, merging, etc...
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		LogEntryIndex OriginalIndex { get; }

		/// <summary>
		///     The (first) line number of the log entry, from 1 to the number of lines in the data source..
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		int LineNumber { get; }

		/// <summary>
		///     The (first) line number of the log entry, from 1 to the number of lines in the original data source..
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		int OriginalLineNumber { get; }

		/// <summary>
		/// The log level of this entry.
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		LevelFlags LogLevel { get; }

		/// <summary>
		///     The timestamp of this log entry, if available.
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		DateTime? Timestamp { get; }

		/// <summary>
		///     The amount of time elapsed between the first and this log entry.
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		TimeSpan? ElapsedTime { get; }

		/// <summary>
		///     The amount of time between this and the previous log entry.
		/// </summary>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		TimeSpan? DeltaTime { get; }

		/// <summary>
		///     Returns the value of this log entry for the given column.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="column"></param>
		/// <returns></returns>
		/// <exception cref="NoSuchColumnException">When this column doesn't exist</exception>
		/// <exception cref="ColumnNotRetrievedException">When this column hasn't been retrieved</exception>
		T GetColumnValue<T>(ILogFileColumn<T> column);
	}
}
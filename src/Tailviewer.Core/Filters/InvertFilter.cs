﻿using System;
using System.Collections.Generic;
using Tailviewer.BusinessLogic.Filters;
using Tailviewer.BusinessLogic.LogFiles;

namespace Tailviewer.Core.Filters
{
	/// <summary>
	///     A filter which wraps another filter and simply inverts its result.
	/// </summary>
	public sealed class InvertFilter : ILogEntryFilter
	{
		private readonly ILogEntryFilter _filter;

		/// <summary>
		///     Initializes this filter.
		/// </summary>
		/// <param name="filter"></param>
		public InvertFilter(ILogEntryFilter filter)
		{
			if (filter == null)
				throw new ArgumentNullException(nameof(filter));

			_filter = filter;
		}

		/// <inheritdoc />
		public bool PassesFilter(IEnumerable<IReadOnlyLogEntry> logEntry)
		{
			return !_filter.PassesFilter(logEntry);
		}

		/// <inheritdoc />
		public bool PassesFilter(IReadOnlyLogEntry logLine)
		{
			return !_filter.PassesFilter(logLine);
		}

		/// <inheritdoc />
		public List<LogLineMatch> Match(IReadOnlyLogEntry line)
		{
			// We don't mark any text because we would have to mark ALL text excluding
			// the actual filter text (since we're the inversion of the inner filter).
			// This is really not helpful and thus we don't mark any text at all...
			return new List<LogLineMatch>();
		}

		/// <inheritdoc />
		public void Match(IReadOnlyLogEntry line, List<LogLineMatch> matches)
		{
			// We don't mark any text because we would have to mark ALL text excluding
			// the actual filter text (since we're the inversion of the inner filter).
			// This is really not helpful and thus we don't mark any text at all...
		}
	}
}
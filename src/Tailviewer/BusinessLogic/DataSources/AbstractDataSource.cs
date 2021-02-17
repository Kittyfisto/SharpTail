﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using log4net;
using Metrolib;
using Tailviewer.Archiver.Plugins.Description;
using Tailviewer.BusinessLogic.Searches;
using Tailviewer.Core;
using Tailviewer.Core.Filters;
using Tailviewer.Core.Properties;
using Tailviewer.Core.Sources;
using Tailviewer.Settings;

namespace Tailviewer.BusinessLogic.DataSources
{
	public abstract class AbstractDataSource
		: IDataSource
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly ITaskScheduler _taskScheduler;
		private readonly LogSourceCounter _counter;
		private readonly TimeSpan _maximumWaitTime;
		private readonly DataSource _settings;
		private readonly LogSourceProxy _logSource;
		private readonly LogSourceSearchProxy _search;

		private readonly LogSourceProxy _findAllLogSource;
		private readonly LogSourceSearchProxy _findAllSearch;

		private ILogSource _filteredLogSource;
		private IEnumerable<ILogEntryFilter> _quickFilterChain;
		private bool _isDisposed;
		private ILogSource _previousUnfilteredLogSource;
		private string _findAllFilter;
		private int? _hideLogLineCount;

		protected AbstractDataSource(ITaskScheduler taskScheduler, DataSource settings, TimeSpan maximumWaitTime)
		{
			if (taskScheduler == null)
				throw new ArgumentNullException(nameof(taskScheduler));
			if (settings == null) throw new ArgumentNullException(nameof(settings));
			if (settings.Id == DataSourceId.Empty) throw new ArgumentException("settings.Id shall be set to an actually generated id");

			_taskScheduler = taskScheduler;
			_settings = settings;
			_maximumWaitTime = maximumWaitTime;
			_counter = new LogSourceCounter();

			_logSource = new LogSourceProxy(taskScheduler, maximumWaitTime);
			_search = new LogSourceSearchProxy(taskScheduler, _logSource, maximumWaitTime);

			_findAllLogSource = new LogSourceProxy(taskScheduler, maximumWaitTime);
			_findAllSearch = new LogSourceSearchProxy(taskScheduler, _findAllLogSource, maximumWaitTime);

			UpdateSearch();
			UpdateFindAllSearch();
		}

		protected ITaskScheduler TaskScheduler => _taskScheduler;

		protected TimeSpan MaximumWaitTime => _maximumWaitTime;

		public ILogSource FilteredLogSource => _logSource;

		public ILogSource FindAllLogSource => _findAllLogSource;

		public ILogSourceSearch FindAllSearch => _findAllSearch;

		public ILogSourceSearch Search => _search;

		public abstract IPluginDescription TranslationPlugin { get; }

		public IEnumerable<ILogEntryFilter> QuickFilterChain
		{
			get { return _quickFilterChain; }
			set
			{
				if (ReferenceEquals(value, _quickFilterChain))
					return;

				_quickFilterChain = value;
				CreateFilteredLogFile();
			}
		}

		public string SearchTerm
		{
			get { return _settings.SearchTerm; }
			set
			{
				if (value == SearchTerm)
					return;

				_settings.SearchTerm = value;
				UpdateSearch();
			}
		}

		public string FindAllFilter
		{
			get { return _findAllFilter; }
			set
			{
				if (value == _findAllFilter)
					return;

				_findAllFilter = value;
				UpdateFindAllLogFile();
				UpdateFindAllSearch();
			}
		}

		public LevelFlags LevelFilter
		{
			get { return _settings.LevelFilter; }
			set
			{
				if (value == LevelFilter)
					return;

				_settings.LevelFilter = value;
				CreateFilteredLogFile();
			}
		}

		public DateTime? LastModified => UnfilteredLogSource.GetProperty(GeneralProperties.LastModified);

		public DateTime LastViewed
		{
			get { return _settings.LastViewed; }
			set { _settings.LastViewed = value; }
		}

		public bool ScreenCleared
		{
			get { return _hideLogLineCount != null; }
		}

		public void ClearScreen()
		{
			_hideLogLineCount = UnfilteredLogSource?.GetProperty(GeneralProperties.LogEntryCount) ?? 0;
			CreateFilteredLogFile();
		}

		public void ShowAll()
		{
			_hideLogLineCount = null;
			CreateFilteredLogFile();
		}

		public DataSourceId Id => _settings.Id;

		public DataSourceId ParentId => _settings.ParentId;

		public void ActivateQuickFilter(QuickFilterId id)
		{
			// Should I add a sanity check here?
			_settings.ActivatedQuickFilters.Add(id);
		}

		public bool DeactivateQuickFilter(QuickFilterId id)
		{
			return _settings.ActivatedQuickFilters.Remove(id);
		}

		public bool IsQuickFilterActive(QuickFilterId id)
		{
			return _settings.ActivatedQuickFilters.Contains(id);
		}

		public abstract ILogSource OriginalLogSource { get; }

		public abstract ILogSource UnfilteredLogSource { get; }

		public int NoLevelCount => _counter.NoLevel.LogEntryCount;

		public int TraceCount => _counter.Trace.LogEntryCount;

		public int DebugCount => _counter.Debugs.LogEntryCount;

		public int InfoCount => _counter.Infos.LogEntryCount;

		public int WarningCount => _counter.Warnings.LogEntryCount;

		public int ErrorCount => _counter.Errors.LogEntryCount;

		public int FatalCount => _counter.Fatals.LogEntryCount;

		public int NoTimestampCount => _counter.NoTimestamp.LogEntryCount;

		public string FullFileName => _settings.File;

		/// <summary>
		/// </summary>
		/// <remarks>
		///     Is not persisted on purpose.
		/// </remarks>
		public string CharacterCode { get; set; }

		public bool FollowTail
		{
			get { return _settings.FollowTail; }
			set { _settings.FollowTail = value; }
		}

		public bool ShowLineNumbers
		{
			get { return _settings.ShowLineNumbers; }
			set { _settings.ShowLineNumbers = value; }
		}

		public bool ShowDeltaTimes
		{
			get { return _settings.ShowDeltaTimes; }
			set { _settings.ShowDeltaTimes = value; }
		}
		
		public bool ShowElapsedTime
		{
			get { return _settings.ShowElapsedTime; }
			set { _settings.ShowElapsedTime = value; }
		}

		public HashSet<LogLineIndex> SelectedLogLines
		{
			get { return _settings.SelectedLogLines; }
			set { _settings.SelectedLogLines = value; }
		}

		public LogLineIndex VisibleLogLine
		{
			get { return _settings.VisibleLogLine; }
			set { _settings.VisibleLogLine = value; }
		}

		public double HorizontalOffset
		{
			get { return _settings.HorizontalOffset; }
			set { _settings.HorizontalOffset = value; }
		}

		public DataSource Settings => _settings;

		public int TotalCount => _counter.Total.LogLineCount;

		public Size? FileSize => UnfilteredLogSource.GetProperty(GeneralProperties.Size);

		public bool ColorByLevel
		{
			get { return _settings.ColorByLevel; }
			set { _settings.ColorByLevel = value; }
		}

		public bool HideEmptyLines
		{
			get { return _settings.HideEmptyLines; }
			set
			{
				if (value == _settings.HideEmptyLines)
					return;

				_settings.HideEmptyLines = value;
				CreateFilteredLogFile();
			}
		}

		public bool IsSingleLine
		{
			get { return _settings.IsSingleLine; }
			set
			{
				if (value == _settings.IsSingleLine)
					return;

				_settings.IsSingleLine = value;
				OnSingleLineChanged();
			}
		}
		
		public void Dispose()
		{
			_logSource.Dispose();
			_search.Dispose();
			_counter.Dispose();

			_findAllLogSource.Dispose();
			_findAllSearch.Dispose();

			_logSource?.Dispose();

			try
			{
				DisposeAdditional();
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception: {0}", e);
			}

			_isDisposed = true;
		}

		/// <summary>
		///     Called during <see cref="Dispose" />, can be implemented to
		///     dispose of additional resources.
		/// </summary>
		protected abstract void DisposeAdditional();

		public bool IsDisposed => _isDisposed;

		public override string ToString()
		{
			return _settings.ToString();
		}

		/// <summary>
		/// Must be called by suclasses when the <see cref="UnfilteredLogSource"/> property changes
		/// (i.e. returns a different object).
		/// </summary>
		protected void OnUnfilteredLogFileChanged()
		{
			_previousUnfilteredLogSource?.RemoveListener(_counter);
			UnfilteredLogSource.AddListener(_counter, TimeSpan.Zero, 1000);
			_previousUnfilteredLogSource = UnfilteredLogSource;

			CreateFilteredLogFile();
		}

		protected abstract void OnSingleLineChanged();

		private void CreateFilteredLogFile()
		{
			_filteredLogSource?.Dispose();

			LevelFlags levelFilter = LevelFilter;
			ILogLineFilter logLineFilter = CreateLogLineFilter();
			ILogEntryFilter logEntryFilter = Filter.Create(levelFilter, _quickFilterChain);
			if (Filter.IsFilter(logEntryFilter) || Filter.IsFilter(logLineFilter))
			{
				_filteredLogSource = UnfilteredLogSource.AsFiltered(_taskScheduler, logLineFilter, logEntryFilter, _maximumWaitTime);
				_logSource.InnerLogSource = _filteredLogSource;
			}
			else
			{
				_filteredLogSource = null;
				_logSource.InnerLogSource = UnfilteredLogSource;
			}
		}

		private ILogLineFilter CreateLogLineFilter()
		{
			var filters = new List<ILogLineFilter>();
			if (HideEmptyLines)
				filters.Add(new EmptyLogLineFilter());
			if (_hideLogLineCount != null)
				filters.Add(new RangeFilter(new LogFileSection(0, _hideLogLineCount.Value)));
			return Filter.Create(filters);
		}

		private void UpdateSearch()
		{
			_search.SearchTerm = SearchTerm;
		}

		private void UpdateFindAllSearch()
		{
			_findAllSearch.SearchTerm = FindAllFilter;
		}

		private void UpdateFindAllLogFile()
		{
			var previous = _findAllLogSource.InnerLogSource;
			previous?.Dispose();

			if (!string.IsNullOrEmpty(_findAllFilter))
			{
				_findAllLogSource.InnerLogSource = new FilteredLogSource(_taskScheduler,
				                                                   MaximumWaitTime,
				                                                   this.UnfilteredLogSource,
				                                                   null,
				                                                   new SubstringFilter(_findAllFilter, ignoreCase: true));
			}
			else
			{
				_findAllLogSource.InnerLogSource = null;
			}
		}
	}
}
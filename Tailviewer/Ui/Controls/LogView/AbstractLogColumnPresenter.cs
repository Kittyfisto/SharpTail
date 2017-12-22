﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Tailviewer.BusinessLogic.LogFiles;
using Tailviewer.Core.LogFiles;

namespace Tailviewer.Ui.Controls.LogView
{
	/// <summary>
	///     Responsible for presenting the values of a particular column.
	/// </summary>
	public abstract class AbstractLogColumnPresenter<T>
		: FrameworkElement
	{
		private readonly ILogFileColumn<T> _column;
		private readonly List<AbstractLogEntryValuePresenter> _values;
		private double _yOffset;

		protected AbstractLogColumnPresenter(ILogFileColumn<T> column)
		{
			if (column == null)
				throw new ArgumentNullException(nameof(column));

			_column = column;
			_values = new List<AbstractLogEntryValuePresenter>();
			ClipToBounds = true;
		}

		protected IEnumerable<AbstractLogEntryValuePresenter> Values => _values;

		/// <summary>
		///     Fetches the newest values for this presenter's column from the given log file.
		/// </summary>
		/// <param name="logFile"></param>
		/// <param name="visibleSection"></param>
		/// <param name="yOffset"></param>
		public void FetchValues(ILogFile logFile, LogFileSection visibleSection, double yOffset)
		{
			if (Visibility != Visibility.Visible) //< We shouldn't waste CPU cycles when we're hidden from view...
				return;

			_yOffset = yOffset;

			_values.Clear();
			if (logFile != null)
			{
				var values = new T[visibleSection.Count];
				logFile.GetColumn(visibleSection, _column, values);
				foreach (var value in values)
					_values.Add(CreatePresenter(value));
			}

			UpdateWidth(logFile);
			InvalidateVisual();
		}

		protected abstract void UpdateWidth(ILogFile logFile);

		/// <summary>
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		protected abstract AbstractLogEntryValuePresenter CreatePresenter(T value);

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.White, pen: null,
			                             rectangle: new Rect(x: 0, y: 0, width: ActualWidth, height: ActualHeight));

			var y = _yOffset;
			foreach (var number in _values)
			{
				number.Render(drawingContext, y, Width);
				y += TextHelper.LineHeight;
			}
		}
	}
}
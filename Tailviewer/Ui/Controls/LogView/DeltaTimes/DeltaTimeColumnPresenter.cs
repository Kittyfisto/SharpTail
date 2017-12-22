﻿using System;
using Tailviewer.BusinessLogic.LogFiles;
using Tailviewer.Core.LogFiles;

namespace Tailviewer.Ui.Controls.LogView.DeltaTimes
{
	/// <summary>
	///     A "canvas" which draws the elapsed time to the previous log entry in the same vertical alignment as <see cref="TextCanvas" />
	///     draws the <see cref="LogLine.Message" />.
	/// </summary>
	public sealed class DeltaTimeColumnPresenter
		: AbstractLogColumnPresenter<TimeSpan?>
	{
		public DeltaTimeColumnPresenter()
			: base(LogFileColumns.DeltaTime)
		{
			Width = 50;
		}

		protected override void UpdateWidth(ILogFile logFile)
		{}

		protected override AbstractLogEntryValuePresenter CreatePresenter(TimeSpan? value)
		{
			return new DeltaTimePresenter(value);
		}
	}
}
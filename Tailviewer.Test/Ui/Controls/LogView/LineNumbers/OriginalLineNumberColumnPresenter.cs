﻿using System.Linq;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tailviewer.BusinessLogic;
using Tailviewer.BusinessLogic.LogFiles;
using Tailviewer.Core.LogFiles;
using Tailviewer.Ui.Controls.LogView.LineNumbers;

namespace Tailviewer.Test.Ui.Controls.LogView.LineNumbers
{
	[TestFixture]
	[RequiresThread(ApartmentState.STA)]
	public sealed class OriginalLineNumberColumnPresenter
	{
		private Tailviewer.Ui.Controls.LogView.LineNumbers.OriginalLineNumberColumnPresenter _column;
		private InMemoryLogFile _logFile;

		[SetUp]
		public void Setup()
		{
			_column = new Tailviewer.Ui.Controls.LogView.LineNumbers.OriginalLineNumberColumnPresenter();

			_logFile = new InMemoryLogFile();
		}

		[Test]
		public void TestUpdateLineNumbers1()
		{
			const int count = 10;
			AddLines(count);

			_column.FetchValues(_logFile, new LogFileSection(0, count), 0);
			var numbers = _column.LineNumbers;
			numbers.Should().NotBeNull();
			numbers.Should().HaveCount(count);
			numbers.Should().Equal(Enumerable.Range(0, count).Select(i => new LineNumberPresenter(i+1)));
		}

		[Test]
		[Description("Verifies that the canvas displays the original line numbers when displaying the section of a filtered log file")]
		public void TestUpdateLineNumbers2()
		{
			var logFile = new Mock<ILogFile>();
			logFile.Setup(x => x.Count).Returns(4);
			logFile.Setup(x => x.OriginalCount).Returns(1000);
			logFile.Setup(x => x.GetColumn(It.Is<LogFileSection>(y => y == new LogFileSection(0, 4)),
			                               It.Is<ILogFileColumn<int>>(y => y == LogFileColumns.OriginalLineNumber),
										   It.IsAny<int[]>(),
			                               It.IsAny<int>()))
				.Callback((LogFileSection section, ILogFileColumn<int> unused, int[] indices, int unused2) =>
				{
					indices[0] = 42;
					indices[1] = 101;
					indices[2] = 255;
					indices[3] = 512;
				});
			_column.FetchValues(logFile.Object, new LogFileSection(0, 4), 0);
			_column.Width.Should().BeApproximately(24.8, 0.1, "because the canvas should reserve space for the original line count, which is 4 digits");
			_column.LineNumbers.Should().Equal(new LineNumberPresenter(42),
				new LineNumberPresenter(101),
				new LineNumberPresenter(255),
				new LineNumberPresenter(512));
		}

		private void AddLines(int count)
		{
			for (int i = 0; i < count; ++i)
			{
				_logFile.AddEntry("", LevelFlags.Fatal);
			}
		}
	}
}
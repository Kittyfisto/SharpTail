﻿using System;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tailviewer.AcceptanceTests.BusinessLogic.LogFiles;
using Tailviewer.BusinessLogic.ActionCenter;
using Tailviewer.BusinessLogic.DataSources;
using Tailviewer.Core.LogFiles;
using Tailviewer.Settings;
using Tailviewer.Ui.ViewModels;

namespace Tailviewer.AcceptanceTests.Ui.ViewModels
{
	[TestFixture]
	public sealed class SingleDataSourceViewModelTest
	{
		private DefaultTaskScheduler _taskScheduler;

		[SetUp]
		public void SetUp()
		{
			_taskScheduler = new DefaultTaskScheduler();
		}

		[Test]
		[Description("Verifies that the number of search results is properly forwarded to the view model upon Update()")]
		public void TestSearch1()
		{
			var settings = new DataSource(TextLogFileAcceptanceTest.File2Mb) { Id = DataSourceId.CreateNew() };
			using (var logFile = new TextLogFile(_taskScheduler, TextLogFileAcceptanceTest.File2Mb))
			using (var dataSource = new SingleDataSource(_taskScheduler, settings, logFile, TimeSpan.Zero))
			{
				var model = new SingleDataSourceViewModel(dataSource, new Mock<IActionCenter>().Object);

				logFile.Property(x => x.EndOfSourceReached).ShouldEventually().BeTrue();

				model.Property(x =>
				{
					x.Update();
					return x.TotalCount;
				}).ShouldEventually().Be(16114);

				//model.Update();
				//model.TotalCount.Should().Be(16114);

				model.SearchTerm = "RPC #12";
				var search = dataSource.Search;
				search.Property(x => x.Count).ShouldEventually().Be(334);

				model.Update();
				model.SearchResultCount.Should().Be(334);
				model.CurrentSearchResultIndex.Should().Be(0);
			}
		}
	}
}
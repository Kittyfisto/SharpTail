﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tailviewer.Api;
using Tailviewer.BusinessLogic.Bookmarks;
using Tailviewer.BusinessLogic.DataSources;
using Tailviewer.Core;
using Tailviewer.Ui.SidePanel.Bookmarks;

namespace Tailviewer.Tests.Ui.Controls.SidePanel.Bookmarks
{
	[TestFixture]
	public sealed class BookmarksViewModelTest
	{
		private BookmarksViewModel _viewModel;
		private Mock<IDataSource> _dataSource;
		private List<Bookmark> _bookmarks;
		private Mock<IDataSources> _dataSources;

		[SetUp]
		public void Setup()
		{
			_bookmarks = new List<Bookmark>();

			_dataSources = new Mock<IDataSources>();
			_dataSources.Setup(x => x.Bookmarks).Returns(_bookmarks);
			_dataSources.Setup(x => x.TryAddBookmark(It.IsAny<IDataSource>(), It.IsAny<LogLineIndex>())).Returns((IDataSource dataSource, LogLineIndex index) => new Bookmark(dataSource, index));

			_dataSource = new Mock<IDataSource>();

			_viewModel = new BookmarksViewModel(_dataSources.Object, bookmark => {});
		}

		[Test]
		public void TestUpdateNoLineSelected()
		{
			_viewModel.CurrentDataSource = _dataSource.Object;
			_viewModel.AddBookmarkCommand.CanExecute(null).Should().BeFalse("because not a single line is selected");

			_dataSource.Setup(x => x.SelectedLogLines).Returns(new HashSet<LogLineIndex> { 42 });
			_viewModel.Update();
			_viewModel.AddBookmarkCommand.CanExecute(null).Should().BeTrue("because a single line is selected and thus it should be possible to add a bookmark there");

			_dataSource.Setup(x => x.SelectedLogLines).Returns(new HashSet<LogLineIndex>());
			_viewModel.Update();
			_viewModel.AddBookmarkCommand.CanExecute(null).Should().BeFalse("because not a single line is selected");
		}

		[Test]
		public void TestRemove1()
		{
			_viewModel.EmptyStatement.Should().BeNull();

			_bookmarks.Add(new Bookmark(_dataSource.Object, 1));
			_viewModel.CurrentDataSource = _dataSource.Object;
			_viewModel.Update();
			_viewModel.Bookmarks.Should().HaveCount(1);
			_viewModel.EmptyStatement.Should().BeNull();

			var bookmark = _viewModel.Bookmarks.First();
			bookmark.RemoveCommand.CanExecute(null).Should().BeTrue();
			new Action(() => bookmark.RemoveCommand.Execute(null)).Should().NotThrow();
			_viewModel.Bookmarks.Should().BeEmpty();
			_dataSources.Verify(x => x.RemoveBookmark(It.IsAny<Bookmark>()), Times.Once);
			_viewModel.EmptyStatement.Should().Be("No Bookmarks added");
		}

		[Test]
		public void TestAddRemove()
		{
			_dataSource.Setup(x => x.SelectedLogLines).Returns(new HashSet<LogLineIndex> {13});
			var logFile = new InMemoryLogSource();
			logFile.AddEmptyEntries(13);
			_dataSource.Setup(x => x.FilteredLogSource).Returns(logFile);

			_viewModel.CurrentDataSource = _dataSource.Object;
			_viewModel.AddBookmarkCommand.Execute(null);
			_viewModel.Bookmarks.Should().NotBeEmpty();
			_viewModel.EmptyStatement.Should().BeNull();

			var bookmark = _viewModel.Bookmarks.First();
			bookmark.RemoveCommand.Execute(null);
			_viewModel.Bookmarks.Should().BeEmpty();
			_viewModel.EmptyStatement.Should().Be("No Bookmarks added");;
		}

		[Test]
		public void TestClearBookmarks()
		{
			_viewModel.RemoveAllBookmarksCommand.CanExecute(null).Should().BeFalse("because there are no bookmarks");
			_viewModel.Update();
			_viewModel.RemoveAllBookmarksCommand.CanExecute(null).Should().BeFalse("because there are no bookmarks");

			_dataSource.Setup(x => x.SelectedLogLines).Returns(new HashSet<LogLineIndex> {13});
			var logFile = new InMemoryLogSource();
			logFile.AddEmptyEntries(13);
			_dataSource.Setup(x => x.FilteredLogSource).Returns(logFile);

			_viewModel.CurrentDataSource = _dataSource.Object;
			_viewModel.AddBookmarkCommand.Execute(null);
			_viewModel.Bookmarks.Should().NotBeEmpty();
			_viewModel.RemoveAllBookmarksCommand.CanExecute(null).Should().BeTrue("because we now have bookmarks");

			_viewModel.RemoveAllBookmarksCommand.Execute(null);
			_viewModel.Bookmarks.Should().BeEmpty();
			_viewModel.RemoveAllBookmarksCommand.CanExecute(null).Should().BeFalse("because there are no bookmarks anymore");
		}
	}
}
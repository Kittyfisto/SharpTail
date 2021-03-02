﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using FluentAssertions;
using NUnit.Framework;
using Tailviewer.Api;
using Tailviewer.Api.Tests;
using Tailviewer.Core;
using Tailviewer.Core.Settings;
using Tailviewer.Settings;

namespace Tailviewer.Tests.Settings
{
	[TestFixture]
	public sealed class ApplicationSettingsTest
	{
		[Test]
		public void TestClone()
		{
			string fname = PathEx.GetTempFileName();
			var settings = new ApplicationSettings(fname);
			settings.LogFile.DefaultEncoding = Encoding.UTF32;

			var clone = settings.Clone();
			clone.Should().NotBeNull();
			clone.Should().NotBeSameAs(settings);
			clone.AutoUpdate.Should().NotBeNull();
			clone.AutoUpdate.Should().NotBeSameAs(settings.AutoUpdate);
			clone.MainWindow.Should().NotBeNull();
			clone.MainWindow.Should().NotBeSameAs(settings.MainWindow);
			clone.DataSources.Should().NotBeNull();
			clone.DataSources.Should().NotBeSameAs(settings.DataSources);
			clone.QuickFilters.Should().NotBeNull();
			clone.QuickFilters.Should().NotBeSameAs(settings.QuickFilters);
			clone.Export.Should().NotBeNull();
			clone.Export.Should().NotBeSameAs(settings.Export);
			clone.LogViewer.Should().NotBeNull();
			clone.LogViewer.Should().NotBeSameAs(settings.LogViewer);
			clone.LogFile.Should().NotBeNull();
			clone.LogFile.Should().NotBeSameAs(settings.LogFile);
			clone.LogFile.DefaultEncoding.Should().Be(Encoding.UTF32);
		}

		[Test]
		[Description("Verifies that the GUID of a data source is written to the file and read again")]
		public void TestDataSourceId()
		{
			string fname = PathEx.GetTempFileName();
			var settings = new ApplicationSettings(fname);
			var dataSource = new DataSource("foo");
			dataSource.Id = DataSourceId.CreateNew();
			settings.DataSources.Add(dataSource);
			settings.Save();

			var settings2 = new ApplicationSettings(fname);
			bool neededPatching;
			settings2.Restore(out neededPatching);
			neededPatching.Should().BeFalse();
			settings2.DataSources.Count.Should().Be(1);
			DataSource dataSource2 = settings2.DataSources[0];
			dataSource2.File.Should().Be("foo");
			dataSource2.Id.Should().Be(dataSource.Id);
		}

		[Test]
		[Description("Verifies that the GUID of a data source's parent is written to the file and read again")]
		public void TestDataSourceParentId()
		{
			string fname = PathEx.GetTempFileName();
			var settings = new ApplicationSettings(fname);
			var dataSource = new DataSource("foo");
			dataSource.Id = DataSourceId.CreateNew();
			dataSource.ParentId = DataSourceId.CreateNew();
			settings.DataSources.Add(dataSource);
			settings.Save();

			var settings2 = new ApplicationSettings(fname);
			bool neededPatching;
			settings2.Restore(out neededPatching);
			neededPatching.Should().BeFalse();
			settings2.DataSources.Count.Should().Be(1);
			DataSource dataSource2 = settings2.DataSources[0];
			dataSource2.File.Should().Be("foo");
			dataSource2.ParentId.Should().Be(dataSource.ParentId);
		}

		[Test]
		[Description("Verifies that a new GUID is created for a DataSource when none is stored in the xml")]
		public void TestRestore2()
		{
			var settings = new ApplicationSettings(@"Settings\settings_no_file_guid.xml");
			bool neededPatching;
			settings.Restore(out neededPatching);
			settings.DataSources.Count.Should().Be(2);
			settings.DataSources[0].Id.Should().NotBe(Guid.Empty);
			settings.DataSources[1].Id.Should().NotBe(Guid.Empty);
			settings.DataSources[0].Id.Should().NotBe(settings.DataSources[1].Id);
			neededPatching.Should().BeTrue("Because we should now be saving the settings again due to the upgrade");
		}

		[Test]
		[Description("Verifies that the last viewed datetime is restored from the file")]
		public void TestRestore3()
		{
			string fname = PathEx.GetTempFileName();
			var settings = new ApplicationSettings(fname);
			DateTime timestamp = DateTime.Now;
			settings.DataSources.Add(new DataSource("dawadaw")
				{
					LastViewed = timestamp
				});

			settings.Save();
			settings = new ApplicationSettings(fname);
			settings.Restore();

			settings.DataSources.Count.Should().Be(1);
			DataSource source = settings.DataSources[0];
			source.Should().NotBeNull();
			source.LastViewed.Should().Be(timestamp);
		}

		[Test]
		[Description("Verifies that the visible log line and horizontal offset is restored")]
		public void TestRestore4()
		{
			string fname = PathEx.GetTempFileName();
			var settings = new ApplicationSettings(fname);
			settings.DataSources.Add(new DataSource("dawadaw")
			{
				VisibleLogLine = 42,
				HorizontalOffset = 142.42
			});

			settings.Save();
			settings = new ApplicationSettings(fname);
			settings.Restore();

			settings.DataSources.Count.Should().Be(1);
			DataSource source = settings.DataSources[0];
			source.Should().NotBeNull();
			source.VisibleLogLine.Should().Be(42);
			source.HorizontalOffset.Should().Be(142.42);
		}

		[Test]
		[Description("Verifies that the folder is created if it doesn't exist")]
		public void TestStore1()
		{
			string path = Path.GetTempPath();
			const string folderName = "ApplicationSettingsTest";
			const string fileName = "settings.xml";
			string fileFolder = Path.Combine(path, folderName);
			string filePath = Path.Combine(fileFolder, fileName);

			if (File.Exists(filePath))
				File.Delete(filePath);

			if (Directory.Exists(fileFolder))
				Directory.Delete(fileFolder);

			var settings = new ApplicationSettings(filePath);
			settings.QuickFilters.Add(new QuickFilter());
			settings.Save().Should().BeTrue();

			File.Exists(filePath);
			var settings2 = new ApplicationSettings(filePath);
			settings2.Restore();
			settings2.QuickFilters.Count.Should().Be(1);
		}

		[Test]
		public void TestStoreRestore()
		{
			const string fileName = "applicationsettingstest.xml";
			var settings = new ApplicationSettings(fileName);
			settings.MainWindow.Left = 1;
			settings.MainWindow.Top = 2;
			settings.MainWindow.Width = 3;
			settings.MainWindow.Height = 4;
			settings.MainWindow.State = WindowState.Maximized;
			settings.DataSources.Add(new DataSource(@"SharpRemote.Host.1600.log")
				{
					Id = DataSourceId.CreateNew(),
					FollowTail = true,
					SearchTerm = "foobar",
					LevelFilter = LevelFlags.Debug,
					VisibleLogLine = new LogLineIndex(1),
					ShowLineNumbers = false,
					HideEmptyLines = true,
					ActivatedQuickFilters =
						{
							QuickFilterId.CreateNew(),
							QuickFilterId.CreateNew(),
							QuickFilterId.CreateNew(),
						}
				});
			settings.DataSources.Add(new DataSource(@"SharpRemote.Host.1700.log")
			{
				Id = DataSourceId.CreateNew(),
				FollowTail = false,
				ShowLineNumbers = true,
				IsSingleLine = true,
			});
			List<QuickFilterId> guids = settings.DataSources[0].ActivatedQuickFilters.ToList();
			settings.QuickFilters.Add(new QuickFilter
				{
					Value = "foobar",
					IgnoreCase = true,
					MatchType = FilterMatchType.RegexpFilter,
					IsInverted = true,
				});
			settings.QuickFilters.Add(new QuickFilter
			{
				Value = "clondykebar",
				IgnoreCase = false,
				MatchType = FilterMatchType.WildcardFilter,
				IsInverted = false,
			});
			QuickFilterId filterId1 = settings.QuickFilters[0].Id;
			QuickFilterId filterId2 = settings.QuickFilters[1].Id;
			settings.DataSources.SelectedItem = settings.DataSources[0].Id;
			settings.Export.ExportFolder = @"C:\hello\world";
			settings.LogFile.DefaultEncoding = Encoding.BigEndianUnicode;
			settings.Save().Should().BeTrue();

			settings = new ApplicationSettings(fileName);
			settings.Restore();
			settings.MainWindow.Left.Should().Be(1);
			settings.MainWindow.Top.Should().Be(2);
			settings.MainWindow.Width.Should().Be(3);
			settings.MainWindow.Height.Should().Be(4);
			settings.MainWindow.State.Should().Be(WindowState.Maximized);
			settings.DataSources.Count.Should().Be(2);
			settings.DataSources[0].File.Should().Be(@"SharpRemote.Host.1600.log");
			settings.DataSources[0].FollowTail.Should().BeTrue();
			settings.DataSources[0].ShowLineNumbers.Should().BeFalse();
			settings.DataSources[0].HideEmptyLines.Should().BeTrue();
			settings.DataSources[0].IsSingleLine.Should().BeFalse();
			settings.DataSources[0].SearchTerm.Should().Be("foobar");
			settings.DataSources[0].LevelFilter.Should().Be(LevelFlags.Debug);
			settings.DataSources[0].VisibleLogLine.Should().Be(new LogLineIndex(1));
			settings.DataSources[0].ActivatedQuickFilters.Should().Equal(guids);

			settings.DataSources[1].File.Should().Be(@"SharpRemote.Host.1700.log");
			settings.DataSources[1].FollowTail.Should().BeFalse();
			settings.DataSources[1].ShowLineNumbers.Should().BeTrue();
			settings.DataSources[1].HideEmptyLines.Should().BeFalse();
			settings.DataSources[1].IsSingleLine.Should().BeTrue();
			settings.DataSources.SelectedItem.Should().NotBe(Guid.Empty);
			settings.DataSources.SelectedItem.Should().Be(settings.DataSources[0].Id);

			settings.QuickFilters.Count.Should().Be(2);
			settings.QuickFilters[0].Id.Should().Be(filterId1);
			settings.QuickFilters[0].Value.Should().Be("foobar");
			settings.QuickFilters[0].IgnoreCase.Should().BeTrue();
			settings.QuickFilters[0].MatchType.Should().Be(FilterMatchType.RegexpFilter);
			settings.QuickFilters[0].IsInverted.Should().BeTrue();

			settings.QuickFilters[1].Id.Should().Be(filterId2);
			settings.QuickFilters[1].Value.Should().Be("clondykebar");
			settings.QuickFilters[1].IgnoreCase.Should().BeFalse();
			settings.QuickFilters[1].MatchType.Should().Be(FilterMatchType.WildcardFilter);
			settings.QuickFilters[1].IsInverted.Should().BeFalse();

			settings.Export.Should().NotBeNull();
			settings.Export.ExportFolder.Should().Be(@"C:\hello\world");

			settings.LogFile.DefaultEncoding.Should().Be(Encoding.BigEndianUnicode);
		}
	}
}
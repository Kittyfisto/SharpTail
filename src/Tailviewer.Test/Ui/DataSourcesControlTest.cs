﻿using System.Collections.ObjectModel;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tailviewer.BusinessLogic.ActionCenter;
using Tailviewer.BusinessLogic.DataSources;
using Tailviewer.BusinessLogic.Sources;
using Tailviewer.Settings;
using Tailviewer.Ui.Controls.DataSourceTree;
using Tailviewer.Ui.ViewModels;

namespace Tailviewer.Test.Ui
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public sealed class DataSourcesControlTest
	{
		private Mock<IActionCenter> _actionCenter;
		private DataSourcesControl _control;
		private ILogFileFactory _logFileFactory;
		private ManualTaskScheduler _scheduler;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_scheduler = new ManualTaskScheduler();
			_logFileFactory = new SimplePluginLogFileFactory(_scheduler);
			_actionCenter = new Mock<IActionCenter>();
		}

		[SetUp]
		public void SetUp()
		{
			_control = new DataSourcesControl();
		}

		[Test]
		public void TestFilter1()
		{
			var sources = new ObservableCollection<IDataSourceViewModel>();
			_control.ItemsSource = sources;
			_control.FilteredItemsSource.Should().BeEmpty();
		}

		[Test]
		public void TestFilter2()
		{
			var sources = new ObservableCollection<IDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;
			_control.FilteredItemsSource.Should().Equal(sources);
		}

		[Test]
		public void TestFilter3()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>();
			_control.ItemsSource = sources;

			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(sources);

			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(sources);

			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(sources);
		}

		[Test]
		public void TestFilter4()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>();
			_control.ItemsSource = sources;

			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));

			sources.RemoveAt(1);
			_control.FilteredItemsSource.Should().Equal(sources);

			sources.RemoveAt(0);
			_control.FilteredItemsSource.Should().Equal(sources);

			sources.RemoveAt(0);
			_control.FilteredItemsSource.Should().Equal(sources);
		}

		[Test]
		public void TestFilter5()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.StringFilter = "2";
			_control.ItemsSource = sources;
			_control.FilteredItemsSource.Should().Equal(sources[1]);
		}

		[Test]
		public void TestFilter6()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.StringFilter = "2";
			_control.ItemsSource = sources;
			_control.FilteredItemsSource.Should().BeEmpty();

			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			sources.Add(
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(sources[1]);
		}

		[Test]
		public void TestFilter7()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;

			_control.StringFilter = "3";
			_control.FilteredItemsSource.Should().Equal(sources[2]);
			sources.RemoveAt(0);
			_control.FilteredItemsSource.Should().Equal(sources[1]);
			sources.RemoveAt(0);
			_control.FilteredItemsSource.Should().Equal(sources[0]);
			sources.RemoveAt(0);
			_control.FilteredItemsSource.Should().BeEmpty();
		}

		[Test]
		public void TestFilter8()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;

			_control.StringFilter = "2";
			_control.FilteredItemsSource.Should().Equal(sources[1]);
			sources.RemoveAt(0);
			_control.FilteredItemsSource.Should().Equal(sources[0]);
			sources.RemoveAt(1);
			_control.FilteredItemsSource.Should().Equal(sources[0]);
			sources.RemoveAt(0);
			_control.FilteredItemsSource.Should().BeEmpty();
		}

		[Test]
		[Description("Verifies that inserting an item at the first position WITHOUT a filter works")]
		public void TestInsertAt1()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;
			sources.Insert(0,
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(sources);
		}

		[Test]
		[Description("Verifies that inserting an item at the last position WITHOUT a filter works")]
		public void TestInsertAt2()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;
			sources.Insert(1,
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(sources);
		}

		[Test]
		[Description("Verifies that inserting an item in the middle WITHOUT a filter works")]
		public void TestInsertAt3()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test1.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;
			sources.Insert(1,
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(sources);
		}

		[Test]
		[Description("Verifies that inserting an item in the middle WITH a filter works")]
		public void TestInsertAt4()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("foo.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;
			// Let's set a filter that causes the first element to be hidden
			_control.StringFilter = "test";
			sources.Insert(1,
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(new object[] {sources[1], sources[2]});
		}

		[Test]
		[Description("Verifies that inserting an item in the middle WITH a filter works")]
		public void TestInsertAt5()
		{
			var sources = new ObservableCollection<SingleDataSourceViewModel>
			{
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test1.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("foo.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object),
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test2.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object)
			};
			_control.ItemsSource = sources;
			// Let's set a filter that causes the first element to be hidden
			_control.StringFilter = "test";
			sources.Insert(2,
				new SingleDataSourceViewModel(new SingleDataSource(_logFileFactory, _scheduler,
					new DataSource("test3.log") {Id = DataSourceId.CreateNew()}), _actionCenter.Object));
			_control.FilteredItemsSource.Should().Equal(new object[] {sources[0], sources[2], sources[3]});

			_control.StringFilter = null;
			_control.FilteredItemsSource.Should().Equal(sources);
		}
	}
}
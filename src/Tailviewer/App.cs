﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Metrolib;
using Tailviewer.BusinessLogic.ActionCenter;
using Tailviewer.BusinessLogic.AutoUpdates;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using Tailviewer.Api;
using Tailviewer.Archiver.Plugins;
using Tailviewer.BusinessLogic.Highlighters;
using Tailviewer.BusinessLogic.LogFileFormats;
using Tailviewer.BusinessLogic.Plugins;
using Tailviewer.BusinessLogic.Sources;
using Tailviewer.Core;
using Tailviewer.Core.Settings;
using Tailviewer.Core.Sources.Text;
using Tailviewer.Settings;
using Tailviewer.Settings.Bookmarks;
using Tailviewer.Ui;
using Tailviewer.Ui.SidePanel.Outline;
using ApplicationSettings = Tailviewer.Settings.ApplicationSettings;
using DataSources = Tailviewer.BusinessLogic.DataSources.DataSources;
using QuickFilters = Tailviewer.BusinessLogic.Filters.QuickFilters;

namespace Tailviewer
{
	public class App
		: Application
	{
		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public App()
		{
			Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Metrolib;component/Themes/Generic.xaml") });
		}

		public static int Start(SingleApplicationHelper.IMutex mutex, string[] args, Stopwatch stopwatch)
		{
			try
			{
				return StartInternal(mutex, args, stopwatch);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception: {0}", e);
				return -1;
			}
		}

		private static int StartInternal(SingleApplicationHelper.IMutex mutex, string[] args, Stopwatch stopwatch)
		{
			InstallExceptionHandlers();
			Log.InfoFormat("Starting {0}...", Constants.ApplicationTitle);
			Log.InfoFormat("Commandline arguments: {0}", string.Join(" ", args));
			LogEnvironment();

			var arguments = ArgumentParser.TryParse(args);
			switch (arguments.Mode)
			{
				case ArgumentParser.Modes.TestLoadPlugin:
					return TestLoadPlugin(arguments.FileToOpen, arguments.PluginInterface);

				default:
					return StartApplication(mutex, arguments.FileToOpen, stopwatch);
			}
		}

		private static int TestLoadPlugin(string pluginToLoad, string pluginInterfaceToLoad)
		{
			InstallConsoleLogger();

			var pluginInterface = typeof(IPlugin).Assembly.GetType(pluginInterfaceToLoad);

			if (pluginToLoad.EndsWith(".tvp", StringComparison.InvariantCultureIgnoreCase))
			{
				var taskScheduler = new DefaultTaskScheduler();
				var filesystem = new Filesystem(taskScheduler);
				using (var loader = new PluginArchiveLoader(filesystem))
				{
					var group = loader.OpenPlugin(pluginToLoad);
					group.Load();
					if (!group.Status.IsLoaded)
						return -1;

					var plugins = group.LoadAllOfType(pluginInterface);
					if (plugins.Count == 0)
					{
						Log.ErrorFormat("The plugin '{0}' doesn't appear to implement '{1}' or there was a problem loading it",
						                pluginToLoad,
						                pluginInterfaceToLoad);
						return -2;
					}

					if (!group.TryLoadAllTypes())
						return -3;

					return 0;
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private static void InstallConsoleLogger()
		{
			var hierarchy = (Hierarchy) LogManager.GetRepository();

			var patternLayout = new PatternLayout
			{
				ConversionPattern = "%-5level: %message%newline"
			};
			patternLayout.ActivateOptions();

			var appender = new ConsoleAppender
			{
				Layout = patternLayout
			};
			appender.ActivateOptions();
			hierarchy.Root.AddAppender(appender);
		}

		private static int StartApplication(SingleApplicationHelper.IMutex mutex, string fileToOpen, Stopwatch stopwatch)
		{
			ApplicationSettings settings = ApplicationSettings.Create();
			settings.Restore(out var neededPatching);
			settings.AllowSave = false; //< We will allow saving once the app is fully booted

			if (neededPatching)
			{
				// TODO: Save settings right again to complete the upgrade
				//       (maybe we should preserve an old version)
			}

			var bookmarks = Bookmarks.Create();
			bookmarks.Restore();

			var services = new ServiceContainer();
			services.RegisterInstance<ILogFileSettings>(settings.LogFile);

			var actionCenter = new ActionCenter();
			using (var taskScheduler = new DefaultTaskScheduler())
			using (var serialTaskScheduler = new SerialTaskScheduler())
			{
				services.RegisterInstance<ITaskScheduler>(taskScheduler);
				services.RegisterInstance<ISerialTaskScheduler>(serialTaskScheduler);
				var navigationService = new NavigationService();
				services.RegisterInstance<INavigationService>(navigationService);

				var filesystem = new Filesystem(taskScheduler);
				services.RegisterInstance<IFilesystem>(filesystem);

				using (var pluginArchiveLoader = new PluginArchiveLoader(filesystem, Constants.PluginPath, Constants.DownloadedPluginsPath))
				{
					var pluginUpdater = new PluginUpdater(pluginArchiveLoader);
					services.RegisterInstance<IPluginUpdater>(pluginUpdater);

					var pluginSystem = CreatePluginSystem(pluginArchiveLoader);
					services.RegisterInstance<IPluginLoader>(pluginSystem);

					var logFileFormatRegistry = new LogFileFormatRegistry(pluginSystem, settings.CustomFormats);
					services.RegisterInstance<ILogFileFormatRepository>(logFileFormatRegistry);
					services.RegisterInstance<ILogFileFormatRegistry>(logFileFormatRegistry);

					var logFileFormatMatcher = new LogFileFormatMatcher(services);
					services.RegisterInstance<ILogFileFormatMatcher>(logFileFormatMatcher);

					var textLogFileParserPlugin = new LogEntryParserFactory(services);
					services.RegisterInstance<ILogEntryParserPlugin>(textLogFileParserPlugin);

					var propertyPresenter = new PropertyPresenterRegistry(pluginSystem);
					services.RegisterInstance<IPropertyPresenterPlugin>(propertyPresenter);

					var fileLogSourceFactory = new RawFileLogSourceFactory(taskScheduler);
					services.RegisterInstance<IRawFileLogSourceFactory>(fileLogSourceFactory);

					var parsingLogSourceFactory = new ParsingLogSourceFactory(services);
					services.RegisterInstance<ILogSourceParserPlugin>(parsingLogSourceFactory);

					var customDataSourcePlugins = pluginSystem.LoadAllOfTypeWithDescription<ICustomDataSourcePlugin>();
					var logFileFactory = new PluginLogSourceFactory(services, customDataSourcePlugins);
					using (var dataSources = new DataSources(logFileFactory, taskScheduler, filesystem, settings.DataSources, bookmarks))
					using (var updater = new AutoUpdater(actionCenter, settings.AutoUpdate))
					{
						if (fileToOpen != null)
						{
							if (File.Exists(fileToOpen))
							{
								// Not only do we want to add this file to the list of data sources,
								// but we also want to select it so the user can view it immediately, regardless
								// of what was selected previously.
								var dataSource = dataSources.AddFile(fileToOpen);
								settings.DataSources.SelectedItem = dataSource.Id;
							}
							else
							{
								Log.ErrorFormat("File '{0}' does not exist, won't open it!", fileToOpen);
							}
						}

						if (settings.AutoUpdate.CheckForUpdates)
						{
							// Our initial check for updates is not due to a user action
							// and therefore we don't need to show a notification when the
							// application is up-to-date.
							updater.CheckForUpdates(addNotificationWhenUpToDate: false);
						}

						var quickFilters = new QuickFilters(settings.QuickFilters);
						var highlighters = new HighlighterCollection();
						services.RegisterInstance<IHighlighters>(highlighters);

						actionCenter.Add(Build.Current);
						actionCenter.Add(Change.Merge(Changelog.MostRecentPatches));
						var application = new App();
						var dispatcher = Dispatcher.CurrentDispatcher;
						var uiDispatcher = new UiDispatcher(dispatcher);
						services.RegisterInstance<IDispatcher>(uiDispatcher);

						dispatcher.UnhandledException += actionCenter.ReportUnhandledException;
						TaskScheduler.UnobservedTaskException += actionCenter.ReportUnhandledException;

						var windowViewModel = new MainWindowViewModel(services,
						                                              settings,
						                                              dataSources,
						                                              quickFilters,
						                                              actionCenter,
						                                              updater);
						navigationService.MainWindow = windowViewModel;

						var window = new MainWindow(settings, windowViewModel);
						
						settings.MainWindow.ClipToBounds(Desktop.Current);
						settings.MainWindow.RestoreTo(window);
						settings.AllowSave = true;

						stopwatch.Stop();
						Log.InfoFormat("Tailviewer started (took {0}ms), showing window...", stopwatch.ElapsedMilliseconds);

						window.Show();
						mutex?.SetListener(window);

						return application.Run();
					}
				}
			}
		}

		private static IPluginLoader CreatePluginSystem(params IPluginLoader[] pluginLoaders)
		{
			// Currently, we deploy some well known "plugins" via the installer and they're
			// not available as *.tvp files just yet (which means the PluginArchiveLoader won't find them).
			// Therefore we register those at a PluginRegistry.
			var wellKnownPlugins = LoadWellKnownPlugins();

			// Even though we're dealing with the limitation above, the rest of the application should not need
			// to care, which is why we make both of those types of plugin accessible from one loader
			var loaders = new List<IPluginLoader>(pluginLoaders);
			loaders.Add(wellKnownPlugins);
			var pluginLoader = new AggregatedPluginLoader(loaders);

			// Last but not least, the PluginArchiveLoader doesn't cache anything which means
			// that multiple Load requests would result in the same plugin being loaded many times.
			// we don't want that (unnecessary work, waste of CPU time, etc..), so that's why there's a cache.
			var pluginCache = new PluginCache(pluginLoader);
			return pluginCache;
		}

		private static IPluginLoader LoadWellKnownPlugins()
		{
			var registry = new PluginRegistry();
			return registry;
		}

		private static ITypeFactory CreateTypeFactory(IPluginLoader pluginLoader)
		{
			var factory = new TypeFactory();
			foreach (var pair in pluginLoader.ResolveSerializableTypes())
			{
				factory.Add(pair.Key, pair.Value);
			}
			factory.Add<Core.Settings.QuickFilters>();
			factory.Add<QuickFilter>();
			factory.Add<QuickFilterId>();
			return factory;
		}

		private static void LogEnvironment()
		{
			var builder = new StringBuilder();
			builder.AppendLine();
			builder.AppendFormat("Tailviewer: v{0}, {1}",
			                     FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion,
			                     Environment.Is64BitProcess ? "64bit" : "32bit");
			builder.AppendLine();

			builder.AppendFormat("Build date: {0}", Constants.BuildDate);
			builder.AppendLine();

			builder.AppendFormat(".NET Environment: {0}", Environment.Version);
			builder.AppendLine();

			builder.AppendFormat("Operating System: {0}, {1}",
			                     Environment.OSVersion,
			                     Environment.Is64BitOperatingSystem ? "64bit" : "32bit");
			builder.AppendLine();

			builder.AppendFormat("Current directory: {0}", Directory.GetCurrentDirectory());

			Log.InfoFormat("Environment: {0}", builder);
		}

		private static void InstallExceptionHandlers()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
		}

		private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			object exception = args.ExceptionObject;

			Log.ErrorFormat("Caught unhandled exception in AppDomain: {0}", exception);

			MessageBox.Show(string.Format("Oops, something went wrong:\r\n{0}", exception),
			                Constants.MainWindowTitle);
		}
	}
}
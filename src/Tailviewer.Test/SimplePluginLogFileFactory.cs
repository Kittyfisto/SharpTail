﻿using System.Linq;
using System.Threading;
using Tailviewer.Archiver.Plugins;
using Tailviewer.BusinessLogic.Sources;
using Tailviewer.Core;
using Tailviewer.Core.Sources.Text;
using Tailviewer.Plugins;

namespace Tailviewer.Test
{
	public sealed class SimplePluginLogFileFactory
		: PluginLogFileFactory
	{
		public SimplePluginLogFileFactory(ITaskScheduler scheduler, params IFileFormatPlugin[] plugins)
			: base(CreateServiceContainer(scheduler), plugins.Select(x => new PluginWithDescription<IFileFormatPlugin>(x, null)), null)
		{}

		private static IServiceContainer CreateServiceContainer(ITaskScheduler scheduler)
		{
			var container = new ServiceContainer();
			container.RegisterInstance<ITaskScheduler>(scheduler);
			container.RegisterInstance<ILogFileFormatMatcher>(new SimpleLogFileFormatMatcher(LogFileFormats.GenericText));
			container.RegisterInstance<ILogEntryParserPlugin>(new SimpleLogEntryParserPlugin());
			container.RegisterInstance<IFileLogSourceFactory>(new FileLogSourceFactory(scheduler));
			container.RegisterInstance<IPluginLoader>(new PluginRegistry());
			container.RegisterInstance<ILogSourceParserPlugin>(new ParsingLogSourceFactory(container));
			return container;
		}
	}
}

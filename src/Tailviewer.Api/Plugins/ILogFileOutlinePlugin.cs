﻿using System.Collections.Generic;
using System.Windows;
using Tailviewer.Ui;

namespace Tailviewer.Plugins
{
	/// <summary>
	///     This interface may be implemented to add an outline to a log file which displays custom content.
	/// </summary>
	/// <remarks>
	///     The outline is part of the right-side panel pane and allows the user to quickly inspect
	///     important information about a log file. Tailviewer will display some generic stats in the outline,
	///     however a plugin will be able to deliver very specific, yet important information here.
	/// </remarks>
	/// <remarks>
	///     Version 1
	///     Initial definition
	/// </remarks>
	/// <remarks>
	///     Version 2
	///     Replaced SupportedFileNames regex with list of supported file formats.
	/// </remarks>
	[PluginInterfaceVersion(version: 2)]
	public interface ILogFileOutlinePlugin
		: IPlugin
	{
		/// <summary>
		///     A list of log file formats supported by this plugin.
		/// </summary>
		/// <remarks>
		///     This can be a well known format known by tailviewer,
		///     a custom format introduced to tailviewer by implementing
		///     <see cref="ILogFileFormatMatcherPlugin" /> or a combination
		///     of the two.
		/// </remarks>
		IReadOnlyList<ILogFileFormat> SupportedFormats { get; }

		/// <summary>
		///     Creates a new view model to represent the given log file.
		/// </summary>
		/// <param name="services">
		///     A service container which may be used to construct types required by this plugin and to inject
		///     custom parameters if necessary
		/// </param>
		/// <param name="logSource"></param>
		/// <returns></returns>
		ILogFileOutlineViewModel CreateViewModel(IServiceContainer services, ILogSource logSource);

		/// <summary>
		///     Creates a new control which presents the given view model.
		/// </summary>
		/// <param name="services">
		///     A service container which may be used to construct types required by this plugin and to inject
		///     custom parameters if necessary
		/// </param>
		/// <param name="viewModel"></param>
		/// <returns></returns>
		FrameworkElement CreateContentPresenterFor(IServiceContainer services, ILogFileOutlineViewModel viewModel);
	}
}
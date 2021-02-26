﻿using System;
using System.Windows.Input;
using System.Windows.Media;
using Tailviewer.Archiver.Plugins.Description;

namespace Tailviewer.Ui.Plugins
{
	public sealed class InstalledPluginViewModel
		: IPluginViewModel
	{
		private readonly IPluginDescription _description;
		//private readonly DelegateCommand _deleteCommand;

		public InstalledPluginViewModel(IPluginDescription description)
		{
			_description = description;

			// TODO: Deleting requires a bit more care because we store plugins
			//       in Program Files which is (for good reason) protected.
			//       Uninstalling a plugin should probably force the UAC dialog
			//       and thus be performed in a separate process. However in order
			//       for this to succeed, we must make sure that we don't hold anymore
			//       handles to the plugin...
			//_deleteCommand = new DelegateCommand();
		}

		public string Name => string.IsNullOrEmpty(_description.Name) ? _description.Id.ToString() : _description.Name;
		public Version Version => _description.Version;
		public string Author => _description.Author ?? "N/A";
		public string Description => _description.Description;
		public string Error => _description.Error;
		public Uri Website => _description.Website;
		public ImageSource Icon => _description.Icon;
		public ICommand DeleteCommand => null;
		public ICommand DownloadCommand => null;

		public bool HasError => !string.IsNullOrEmpty(_description.Error);
	}
}
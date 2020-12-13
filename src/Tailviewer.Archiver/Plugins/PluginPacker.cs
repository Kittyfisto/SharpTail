﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using log4net;
using PE;
using Tailviewer.Archiver.Plugins.Description;

namespace Tailviewer.Archiver.Plugins
{
	/// <summary>
	///     Responsible for creating a plugin package which can then be dropped into Tailviewer's plugin folder.
	/// </summary>
	/// <remarks>
	///     Plugin authors do not need to this class directly, archiver.exe should be more convenient to use in many cases.
	/// </remarks>
	public sealed class PluginPacker
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		///     The maximum size of an icon's edge (height and/or width) in pixels.
		/// </summary>
		private const int MaximumIconSize = 48;

		/// <summary>
		///     The maximum supported .NET Framework version a plugin may target.
		///     This is limited to the same version that Tailviewer.exe is built against.
		///     Currently, this happens to be .NET Framework 4.7.1
		/// </summary>
		private static readonly Version MaximumNetFrameworkVersion = new Version(4, 7, 1);

		private readonly ZipArchive _archive;
		private readonly PluginPackageIndex _index;
		private bool _disposed;
		private string _currentDirectory;

		private PluginPacker(ZipArchive archive)
		{
			if (archive == null)
				throw new ArgumentNullException(nameof(archive));

			_archive = archive;
			_index = new PluginPackageIndex
			{
				PluginArchiveVersion = PluginArchive.CurrentPluginArchiveVersion,
				Assemblies = new List<AssemblyDescription>(),
				NativeImages = new List<NativeImageDescription>(),
				Version = new Version(0, 0, 0).ToString()
			};
		}

		public string Version => _index.Version;

		/// <inheritdoc />
		public void Dispose()
		{
			if (!_disposed)
			{
				StoreIndex();
				_archive.Dispose();
				_disposed = true;
			}
		}

		/// <summary>
		///     Adds a Tailviewer plugin (which is a .NET assembly with special content) to the plugin archive.
		///     There can be only one plugin assembly per archive.
		/// </summary>
		/// <param name="pluginFilePath"></param>
		public void AddPluginAssembly(string pluginFilePath)
		{
			try
			{
				_currentDirectory = Path.GetDirectoryName(pluginFilePath) ?? Directory.GetCurrentDirectory();
				var assembly = AddAssembly(PluginArchive.PluginAssemblyEntryName, pluginFilePath);
				var assemblyLoader = new PluginAssemblyLoader();
				var description = assemblyLoader.ReflectPlugin(assembly, pluginFilePath);
				UpdateIndex(description);
			}
			finally
			{
				_currentDirectory = null;
			}
		}
		
		/// <summary>
		///     Adds a new file to the  plugin package.
		/// </summary>
		/// <param name="entryName"></param>
		/// <param name="fileName"></param>
		public void AddFile(string entryName, string fileName)
		{
			try
			{
				_currentDirectory = Path.GetDirectoryName(fileName) ?? Directory.GetCurrentDirectory();
				using (var stream = File.OpenRead(fileName))
				{
					AddFile(entryName, stream);
				}
			}
			finally
			{
				_currentDirectory = null;
			}
		}

		/// <summary>
		///     Adds a new file to the plugin package.
		/// </summary>
		/// <param name="entryName"></param>
		/// <param name="content"></param>
		public void AddFile(string entryName, Stream content)
		{
			Log.InfoFormat("Adding file '{0}'...", entryName);

			PeHeader header;
			PortableExecutable.TryReadHeader(content, out header, leaveOpen: true);
			content.Position = 0;
			if (header != null)
			{
				if (header.IsClrAssembly)
				{
					AddAssembly(entryName, content, header);
				}
				else
				{
					AddNativeImage(entryName, content, header);
					
				}
			}
			else
			{
				AddFileRaw(entryName, content);
			}
		}

		/// <summary>
		///     Sets the icon for the plugin archive to the given one.
		///     By default, a plugin doesn't have an icon.
		/// </summary>
		/// <remarks>
		///     A plugin archive can only have one icon.
		/// </remarks>
		/// <param name="icon"></param>
		public void SetIcon(Stream icon)
		{
			Log.InfoFormat("Adding icon...");

			using (var image = new Bitmap(icon))
			{
				// TODO: Include rescaling the icon if we don't like its size...
				AddFile(PluginArchive.IconEntryName, icon);
			}
		}

		public void SetChanges(string changesFileName)
		{
			try
			{
				var content = File.ReadAllBytes(changesFileName);
				var stream = new MemoryStream(content);
				var changes = SerializableChanges.Deserialize(stream); //< We want to make sure that the file is legit..
				if (!changes.Changes.Any())
					Log.WarnFormat("The changelist file '{0}' doesn't contain any changes!", changesFileName);

				stream.Position = 0;
				AddFile(PluginArchive.ChangesName, stream);
			}
			catch (Exception e)
			{
				throw new Exception($"Unable to read changelist: {e.Message}", e);
			}
		}

		/// <summary>
		///     Adds a .NET assembly to the plugin archive.
		/// </summary>
		/// <param name="entryName">The relative name of the resulting file in the archive</param>
		/// <param name="assemblyFileName"></param>
		private Assembly AddAssembly(string entryName, string assemblyFileName)
		{
			using (var content = File.OpenRead(assemblyFileName))
			{
				PeHeader header;
				PortableExecutable.TryReadHeader(content, out header, leaveOpen: true);
				content.Position = 0;
				return AddAssembly(entryName, content, header);
			}
		}

		/// <summary>
		///     Adds a .NET assembly to the plugin archive.
		/// </summary>
		/// <param name="entryName">The relative name of the resulting file in the archive</param>
		/// <param name="content"></param>
		/// <param name="header"></param>
		private Assembly AddAssembly(string entryName, Stream content, PeHeader header)
		{
			if (!header.Is32BitHeader)
				throw new PackException("Assemblies must be compiled for x86 or AnyCPU");

			byte[] rawAssembly;
			var assembly = LoadAssemblyFrom(content, out rawAssembly);
			var targetFramework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();
			var version = ParseVersion(targetFramework);
			// This attribute has been added with .NET 4.0. Tailviewer should obviously
			// support adding assemblies which target older .NET frameworks where this attribute
			// is obviously missing and therefore we will tolerate assemblies without it.
			if (version != null)
				if (version > MaximumNetFrameworkVersion)
					throw new PackException($"Assemblies may only target frameworks of up to .NET {MaximumNetFrameworkVersion}");

			var assemblyDescription = AssemblyDescription.FromAssembly(assembly);
			assemblyDescription.EntryName = entryName;
			AddFile(entryName, rawAssembly);
			_index.Assemblies.Add(assemblyDescription);

			foreach (var dependency in assemblyDescription.Dependencies)
			{
				var assemblyName = new AssemblyName(dependency.FullName);
				if (ShouldAddDependency(assemblyName))
				{
					var fileName = Path.Combine(_currentDirectory, string.Format("{0}.exe", assemblyName.Name));
					if (!File.Exists(fileName))
						fileName = Path.Combine(_currentDirectory, string.Format("{0}.dll", assemblyName.Name));
					var dependencyEntryName = Path.GetFileName(fileName);
					AddAssembly(dependencyEntryName, fileName);
				}
			}

			return assembly;
		}

		/// <summary>
		///     Adds a new file to the  plugin package.
		/// </summary>
		/// <param name="entryName"></param>
		/// <param name="content"></param>
		private void AddFile(string entryName, byte[] content)
		{
			var entry = _archive.CreateEntry(entryName, CompressionLevel.NoCompression);
			using (var stream = entry.Open())
			{
				stream.Write(content, 0, content.Length);
			}
		}

		/// <summary>
		/// Parses the .NET version from the given attribute name.
		/// </summary>
		/// <param name="targetFramework"></param>
		/// <returns></returns>
		private Version ParseVersion(TargetFrameworkAttribute targetFramework)
		{
			var name = targetFramework?.FrameworkName;
			if (name == null)
				return null;

			const string pattern = "Version=v";
			int idx = name.IndexOf(pattern);
			if (idx == -1)
				return null;

			idx += pattern.Length;
			var versionString = name.Substring(idx);
			Version version;
			if (!System.Version.TryParse(versionString, out version))
				return null;

			return version;
		}

		private void AddNativeImage(string entryName, Stream content, PeHeader header)
		{
			if (!header.Is32BitHeader)
				throw new PackException("Native images must be compiled for x86");

			var description = new NativeImageDescription
			{
				EntryName = entryName,
				ImageName = Path.GetFileNameWithoutExtension(entryName)
			};
			_index.NativeImages.Add(description);

			AddFileRaw(entryName, content);
		}

		private bool ShouldAddDependency(AssemblyName dependency)
		{
			if (_index.Assemblies.Any(x => x.AssemblyName == dependency.Name))
			{
				Log.DebugFormat("Assembly '{0}' has already been added to the archive, skipping it...", dependency);
				return false;
			}

			switch (dependency.Name)
			{
				case "CommandLine":
				case "log4net":
				case "Metrolib":
				case "System.Extensions":
				case "Tailviewer.Api":
				case "Tailviewer.Core":
				case "PE":
					return false;

				default:
					var assembly = Assembly.Load(dependency);
					var attribute = assembly.GetCustomAttribute<AssemblyProductAttribute>();
					if (attribute == null)
						return true;

					if (attribute.Product == "Microsoft® .NET Framework")
						return false;

					return true;
			}
		}

		private void AddFileRaw(string entryName, Stream content)
		{
			var entry = _archive.CreateEntry(entryName, CompressionLevel.NoCompression);
			using (var stream = entry.Open())
			using (var writer = new BinaryWriter(stream))
			{
				var buffer = new byte[4096];
				int read;
				while ((read = content.Read(buffer, 0, buffer.Length)) > 0)
					writer.Write(buffer, 0, read);
			}
		}

		private void UpdateIndex(IPluginDescription description)
		{
			_index.Description = description.Description;
			_index.Id = description.Id.Value;
			_index.Name = description.Name;
			_index.Author = description.Author;
			_index.Website = description.Website != null ? description.Website.ToString() : null;
			_index.Version = description.Version?.ToString();
			_index.ImplementedPluginInterfaces = new List<PluginInterfaceImplementation>();
			foreach (var implementationDescription in description.PluginImplementations)
			{
				_index.ImplementedPluginInterfaces.Add(new PluginInterfaceImplementation
				{
					InterfaceTypename = implementationDescription.InterfaceType.FullName,
					ImplementationTypename = implementationDescription.FullTypeName,
					InterfaceVersion = implementationDescription.Version.Value
				});
			}
			_index.SerializableTypes = new List<SerializableTypeDescription>();
			foreach (var pair in description.SerializableTypes)
			{
				_index.SerializableTypes.Add(new SerializableTypeDescription
				{
					Name = pair.Key,
					FullName = pair.Value
				});
			}

			_index.TailviewerApiVersion = description.TailviewerApiVersion?.ToString();
		}

		private void StoreIndex()
		{
			using (var stream = new MemoryStream())
			{
				using (var writer = new StreamWriter(stream, Encoding.UTF8, 4086, true))
				{
					var serializer = new XmlSerializer(typeof(PluginPackageIndex));
					serializer.Serialize(writer, _index);
				}

				stream.Position = 0;
				AddFile(PluginArchive.IndexEntryName, stream);
			}
		}

		private Assembly LoadAssemblyFrom(Stream assemblyContent, out byte[] rawAssembly)
		{
			rawAssembly = new byte[assemblyContent.Length - assemblyContent.Position];
			int offset = 0;
			int toRead;
			while ((toRead = Math.Min(4096, rawAssembly.Length - offset)) > 0)
			{
				offset += assemblyContent.Read(rawAssembly, offset, toRead);
			}
			return Assembly.Load(rawAssembly);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="leaveOpen"></param>
		/// <returns></returns>
		public static PluginPacker Create(Stream stream, bool leaveOpen = false)
		{
			return new PluginPacker(new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen, Encoding.UTF8));
		}
	}
}
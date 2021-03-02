﻿using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using Tailviewer.Api;
using Tailviewer.BusinessLogic.Exporter;
using Tailviewer.Core.Sources;

namespace Tailviewer.Tests.BusinessLogic.Export
{
	[TestFixture]
	public sealed class LogFileToFileExporterTest
	{
		private string _directory;

		[SetUp]
		public void Setup()
		{
			_directory = Path.Combine(Path.GetTempPath(), "Tailviewer", "Tests", "Export");
			if (Directory.Exists(_directory))
				Directory.Delete(_directory, true);
		}

		[Test]
		public void TestExportTwoLines()
		{
			var logFile = new InMemoryLogSource();
			logFile.AddEntry("Hello,", LevelFlags.Other);
			logFile.AddEntry("World!", LevelFlags.Other);
			var exporter = new LogFileToFileExporter(logFile, _directory, "foo");
			exporter.FullExportFilename.Should().BeNull("because the full filename must be determined from inside Export, NOT beforehand");
			new Action(() => exporter.Export()).Should().NotThrow();
			exporter.FullExportFilename.Should().NotBeNull();
			exporter.FullExportFilename.Should().StartWith(_directory);

			GetString(exporter.FullExportFilename).Should().Be("Hello,\r\nWorld!");
		}

		[Test]
		public void TestExportTwice()
		{
			var logFile = new InMemoryLogSource();
			logFile.AddEntry("Hello", LevelFlags.Other);
			var exporter1 = new LogFileToFileExporter(logFile, _directory, "foo");
			exporter1.Export();

			logFile.AddEntry("World!", LevelFlags.Other);
			var exporter2 = new LogFileToFileExporter(logFile, _directory, "foo");
			new Action(() => exporter2.Export()).Should().NotThrow();

			exporter1.FullExportFilename.Should()
				.NotBe(exporter2.FullExportFilename,
					"because previous exports should not be overwritten");

			GetString(exporter1.FullExportFilename).Should().Be("Hello");
			GetString(exporter2.FullExportFilename).Should().Be("Hello\r\nWorld!");
		}

		private static string GetString(string fileName)
		{
			File.Exists(fileName).Should().BeTrue("because Export() should've created a file on disk");
			return File.ReadAllText(fileName);
		}
	}
}
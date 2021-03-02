﻿using System;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Tailviewer.Api;
using Tailviewer.BusinessLogic.ActionCenter;
using Tailviewer.BusinessLogic.FileExplorer;

namespace Tailviewer.Tests.BusinessLogic.ActionCenter
{
	[TestFixture]
	public class OpenFolderActionTest
	{
		[Test]
		public void TestCtor1()
		{
			Action a = new Action(() => new OpenFolderAction(null, new Mock<IFileExplorer>().Object));
			a.Should().Throw<ArgumentNullException>("because path isn't set");
		}

		[Test]
		public void TestCtor2()
		{
			Action a = new Action(() => new OpenFolderAction("a", null));
			a.Should().Throw<ArgumentNullException>("because file explorer isn't set");
		}

		[Test]
		public void TestOpenFolder()
		{
			var fileExplorer = new Mock<IFileExplorer>();
			fileExplorer.Setup(f => f.SelectFile(It.IsAny<string>()))
				.Throws<DirectoryNotFoundException>();

			var openFolderAction = new OpenFolderAction("unknown directory", fileExplorer.Object);
			openFolderAction.Property(x => x.Progress).ShouldEventually().Be(Percentage.HundredPercent);
		}
	}
}
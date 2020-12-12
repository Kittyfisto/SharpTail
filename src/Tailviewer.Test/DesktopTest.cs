﻿using FluentAssertions;
using Metrolib;
using NUnit.Framework;
using Tailviewer.Settings;
using Size = System.Windows.Size;

namespace Tailviewer.Test
{
	/// <summary>
	/// 
	/// </summary>
	[TestFixture]
	public sealed class DesktopTest
	{
		[Test]
		public void TestOneMonitorWindowFits()
		{
			var window = new WindowSettings {Left = 10, Top = 20, Width = 800, Height = 600};

			var screen = new Desktop.Screen(new Desktop.Point(0, 0), new Size(1024, 768));
			var desktop = new Desktop(new[] {screen});
			var actualWindow = desktop.ClipToBoundaries(window);
			actualWindow.Should().BeSameAs(window);
			actualWindow.Left.Should().Be(10);
			actualWindow.Top.Should().Be(20);
			actualWindow.Width.Should().Be(800);
			actualWindow.Height.Should().Be(600);
		}

		[Test]
		public void TestOneMonitorWindowClipsLeft()
		{
			var window = new WindowSettings {Left = -1, Top = 20, Width = 800, Height = 600};

			var screen = new Desktop.Screen(new Desktop.Point(0, 0), new Size(1024, 768));
			var desktop = new Desktop(new[] {screen});
			var actualWindow = desktop.ClipToBoundaries(window);
			actualWindow.Should().NotBeSameAs(window);
			actualWindow.Left.Should().Be(112, "because the window should've been moved to the center");
			actualWindow.Top.Should().Be(84, "because the window should've been moved to the center");
			actualWindow.Width.Should().Be(800);
			actualWindow.Height.Should().Be(600);
		}

		[Test]
		public void TestOneMonitorWindowClipsTop()
		{
			var window = new WindowSettings {Left = 10, Top = -1, Width = 800, Height = 600};

			var screen = new Desktop.Screen(new Desktop.Point(0, 0), new Size(1024, 768));
			var desktop = new Desktop(new[] {screen});
			var actualWindow = desktop.ClipToBoundaries(window);
			actualWindow.Should().NotBeSameAs(window);
			actualWindow.Left.Should().Be(112, "because the window should've been moved to the center");
			actualWindow.Top.Should().Be(84, "because the window should've been moved to the center");
			actualWindow.Width.Should().Be(800);
			actualWindow.Height.Should().Be(600);
		}

		[Test]
		public void TestOneMonitorWindowClipsRight()
		{
			var window = new WindowSettings {Left = 225, Top = 20, Width = 800, Height = 600};

			var screen = new Desktop.Screen(new Desktop.Point(0, 0), new Size(1024, 768));
			var desktop = new Desktop(new[] {screen});
			var actualWindow = desktop.ClipToBoundaries(window);
			actualWindow.Should().NotBeSameAs(window);
			actualWindow.Left.Should().Be(112, "because the window should've been moved to the center");
			actualWindow.Top.Should().Be(84, "because the window should've been moved to the center");
			actualWindow.Width.Should().Be(800);
			actualWindow.Height.Should().Be(600);
		}

		[Test]
		public void TestOneMonitorWindowClipsBottom()
		{
			var window = new WindowSettings {Left = 10, Top = 169, Width = 800, Height = 600};

			var screen = new Desktop.Screen(new Desktop.Point(0, 0), new Size(1024, 768));
			var desktop = new Desktop(new[] {screen});
			var actualWindow = desktop.ClipToBoundaries(window);
			actualWindow.Should().NotBeSameAs(window);
			actualWindow.Left.Should().Be(112, "because the window should've been moved to the center");
			actualWindow.Top.Should().Be(84, "because the window should've been moved to the center");
			actualWindow.Width.Should().Be(800);
			actualWindow.Height.Should().Be(600);
		}

		[Test]
		public void TestOneMonitorWindowTooBig()
		{
			var window = new WindowSettings {Left = 10, Top = 169, Width = 1024, Height = 768};

			var screen = new Desktop.Screen(new Desktop.Point(0, 0), new Size(800, 600));
			var desktop = new Desktop(new[] {screen});
			var actualWindow = desktop.ClipToBoundaries(window);
			actualWindow.Should().NotBeSameAs(window);
			actualWindow.Left.Should().Be(0, "because the window should've been moved to the center");
			actualWindow.Top.Should().Be(0, "because the window should've been moved to the center");
			actualWindow.Width.Should().Be(800);
			actualWindow.Height.Should().Be(600);
		}
	}
}

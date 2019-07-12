﻿using FluentAssertions;
using NUnit.Framework;
using Tailviewer.Archiver.Repository;

namespace Tailviewer.Archiver.Test.Repository
{
	[TestFixture]
	public sealed class RemotePublishDisabledExceptionTest
	{
		[Test]
		public void TestRoundtrip()
		{
			BinaryFormatterExtensions.Roundtrip(new RemotePublishDisabledException())
			                          .Should().BeOfType<RemotePublishDisabledException>();
		}
	}
}

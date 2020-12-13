﻿using System;
using System.Windows.Media;
using System.Xml;
using Metrolib;

namespace Tailviewer.Core
{
	/// <summary>
	///     Extension methods for the <see cref="XmlWriter" /> class.
	/// </summary>
	public static class XmlWriterExtensions
	{
		/// <summary>
		///     Writes the given id into an attribute with the given name.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="localName"></param>
		/// <param name="id"></param>
		public static void WriteAttribute(this XmlWriter writer, string localName, DataSourceId id)
		{
			writer.WriteAttributeGuid(localName, id.Value);
		}

		/// <summary>
		///     Writes the given id into an attribute with the given name.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="localName"></param>
		/// <param name="id"></param>
		public static void WriteAttribute(this XmlWriter writer, string localName, QuickFilterId id)
		{
			writer.WriteAttributeGuid(localName, id.Value);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="localName"></param>
		/// <param name="color"></param>
		public static void WriteAttributeColor(this XmlWriter writer, string localName, Color color)
		{
			var colorVlaue = color.ToString();
			writer.WriteAttributeString(localName, colorVlaue);
		}
	}
}
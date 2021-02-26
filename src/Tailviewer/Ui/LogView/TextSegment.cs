﻿using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Tailviewer.Settings;

namespace Tailviewer.Ui.LogView
{
	public struct TextSegment
	{
		public TextSegment(string value, Brush foregroundBrush, bool isRegular, TextSettings textSettings)
		{
			FormattedText = new FormattedText(Prepare(value),
			                                  CultureInfo.CurrentUICulture,
			                                  FlowDirection.LeftToRight,
			                                  textSettings.Typeface,
			                                  textSettings.FontSize,
			                                  foregroundBrush,
			                                  1.25);
			FormattedText.LineHeight = textSettings.LineHeight;
			//Width = TextHelper.EstimateWidthUpperLimit(value);
			Width = FormattedText.WidthIncludingTrailingWhitespace;
			IsRegular = isRegular;
		}

		public string Text => FormattedText.Text;

		public override string ToString()
		{
			return Text;
		}

		/// <summary>
		/// The estimation of TextHelper.EstimateWidthUpperLimit is way off
		/// when there are tabs in the text. This poses a problem when drawing multiple
		/// segments per line because the offsets are way off (text would overlap each other).
		/// Replacing tabs with spaces is currently the simplest option to deal with this...
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		private static string Prepare(string value)
		{
			return value.Replace('\t', ' ');
		}

		public readonly FormattedText FormattedText;
		public readonly bool IsRegular;
		public readonly double Width;
	}
}
﻿using System;
using System.Collections.Generic;
using Tailviewer.Api;

namespace Tailviewer.Core.Filters.ExpressionEngine
{
	internal sealed class TimestampVariable
		: IExpression<DateTime?>
	{
		public const string Value = "timestamp";

		#region Implementation of IExpression

		public Type ResultType => typeof(DateTime);

		public DateTime? Evaluate(IReadOnlyList<IReadOnlyLogEntry> logEntry)
		{
			using (var it = logEntry.GetEnumerator())
			{
				if (!it.MoveNext())
					return null;

				return it.Current.Timestamp;
			}
		}

		object IExpression.Evaluate(IReadOnlyList<IReadOnlyLogEntry> logEntry)
		{
			return Evaluate(logEntry);
		}

		#endregion

		#region Overrides of Object

		public override bool Equals(object obj)
		{
			return obj is TimestampVariable;
		}

		public override int GetHashCode()
		{
			return 103;
		}

		public override string ToString()
		{
			return string.Format("{0}{1}", Tokenizer.ToString(TokenType.Dollar), Value);
		}

		#endregion
	}
}
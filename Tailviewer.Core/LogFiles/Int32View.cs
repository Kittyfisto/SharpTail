using System.Collections;
using System.Collections.Generic;
using Tailviewer.BusinessLogic;

namespace Tailviewer.Core.LogFiles
{
	/// <summary>
	///     Treats a <see cref="IReadOnlyList{LogLineIndex}" /> as an <see cref="IReadOnlyList{Int32}" />.
	/// </summary>
	internal sealed class Int32View
		: IReadOnlyList<int>
	{
		private readonly IReadOnlyList<LogLineIndex> _source;

		public Int32View(IReadOnlyList<LogLineIndex> source)
		{
			_source = source;
		}

		public IEnumerator<int> GetEnumerator()
		{
			return new Enumerator(_source.GetEnumerator());
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public int Count => _source.Count;

		public int this[int index] => (int) _source[index];

		private sealed class Enumerator
			: IEnumerator<int>
		{
			private readonly IEnumerator<LogLineIndex> _source;

			public Enumerator(IEnumerator<LogLineIndex> source)
			{
				_source = source;
			}

			public void Dispose()
			{
				_source.Dispose();
			}

			public bool MoveNext()
			{
				return _source.MoveNext();
			}

			public void Reset()
			{
				_source.Reset();
			}

			public int Current => (int) _source.Current;

			object IEnumerator.Current => Current;
		}
	}
}
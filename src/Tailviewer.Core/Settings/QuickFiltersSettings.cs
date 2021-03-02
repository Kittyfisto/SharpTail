﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Tailviewer.Api;

// ReSharper disable once CheckNamespace
namespace Tailviewer.Core
{
	/// <summary>
	///     The list of all application-wide quick filters.
	/// </summary>
	public sealed class QuickFiltersSettings
		: List<QuickFilterSettings>
		, ISerializableType
		, ICloneable
	{
		private TimeFilterSettings _timeFilter;

		/// <summary>
		/// 
		/// </summary>
		public QuickFiltersSettings()
		{
			_timeFilter = new TimeFilterSettings();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		/// <summary>
		/// 
		/// </summary>
		public TimeFilterSettings TimeFilter => _timeFilter;

		/// <summary>
		///     Restores the values of this object from the given xml document.
		/// </summary>
		/// <param name="reader"></param>
		public void Restore(XmlReader reader)
		{
			var quickfilters = new List<QuickFilterSettings>();
			var subtree = reader.ReadSubtree();
			while (subtree.Read())
				switch (subtree.Name)
				{
					case "quickfilter":
						var quickfilter = new QuickFilterSettings();
						if (quickfilter.Restore(subtree))
							quickfilters.Add(quickfilter);
						break;

					case "timefilter":
						_timeFilter.Restore(subtree);
						break;
				}

			Clear();
			AddRange(quickfilters);
		}

		/// <summary>
		///     Stores the values of this object in the given xml document.
		/// </summary>
		/// <param name="writer"></param>
		public void Save(XmlWriter writer)
		{
			foreach (var dataSource in this)
			{
				writer.WriteStartElement("quickfilter");
				dataSource.Save(writer);
				writer.WriteEndElement();
			}

			writer.WriteStartElement("timefilter");
			_timeFilter.Save(writer);
			writer.WriteEndElement();
		}

		/// <summary>
		///     Returns a deep clone of this object.
		/// </summary>
		/// <returns></returns>
		public QuickFiltersSettings Clone()
		{
			var filters = new QuickFiltersSettings();
			filters.AddRange(this.Select(x => x.Clone()));
			filters._timeFilter = _timeFilter.Clone();
			return filters;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(obj, objB: null))
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			var other = obj as QuickFiltersSettings;
			if (ReferenceEquals(other, objB: null))
				return false;

			if (Count != other.Count)
				return false;

			for (var i = 0; i < Count; ++i)
			{
			}

			return true;
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			return 42;
		}

		/// <summary>
		///     Tests if this and the given object are equivalent (i.e. would behave identical, or not).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool IsEquivalent(QuickFiltersSettings other)
		{
			if (ReferenceEquals(other, objB: null))
				return false;
			if (ReferenceEquals(this, other))
				return true;
			if (Count != other.Count)
				return false;

			for (var i = 0; i < Count; ++i)
			{
				var filter = this[i];
				var otherFilter = other[i];

				if (!IsEquivalent(filter, otherFilter))
					return false;
			}

			return true;
		}

		private static bool IsEquivalent(QuickFilterSettings lhs, QuickFilterSettings rhs)
		{
			if (ReferenceEquals(lhs, rhs))
				return true;
			if (ReferenceEquals(lhs, objB: null))
				return false;

			return lhs.IsEquivalent(rhs);
		}

		/// <inheritdoc />
		public void Serialize(IWriter writer)
		{
			writer.WriteAttribute("QuickFilters", (IEnumerable<QuickFilterSettings>)this);
		}

		/// <inheritdoc />
		public void Deserialize(IReader reader)
		{
			Clear();
			if (reader.TryReadAttribute("QuickFilters", out IEnumerable<QuickFilterSettings> filters))
				AddRange(filters);
		}
	}
}
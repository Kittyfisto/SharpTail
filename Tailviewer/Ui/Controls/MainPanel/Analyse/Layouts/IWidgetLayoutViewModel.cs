using System;
using System.ComponentModel;
using Tailviewer.Ui.Analysis;
using Tailviewer.Ui.Controls.MainPanel.Analyse.SidePanels;

namespace Tailviewer.Ui.Controls.MainPanel.Analyse.Layouts
{
	/// <summary>
	///     Responsible for controlling *how* a list of widgets is displayed as well as for
	///     allowing the user to add new widgets and rearrange existing widgets.
	/// </summary>
	/// <remarks>
	///     It is expected that the accompanying control takes care of dropping <see cref="WidgetFactoryViewModel" />s
	///     as well as <see cref="IWidgetViewModel" />s.
	/// </remarks>
	public interface IWidgetLayoutViewModel
		: INotifyPropertyChanged
	{
		/// <summary>
		///     Adds the given widget to this layout.
		/// </summary>
		/// <param name="widget"></param>
		void Add(IWidgetViewModel widget);

		/// <summary>
		///     Removes the given widget from this layout.
		/// </summary>
		/// <param name="widget"></param>
		void Remove(IWidgetViewModel widget);

		/// <summary>
		///     This event is fired when this layout requests that the given widget shall
		///     be added. It is expected that handlers of this event call <see cref="Add" />
		///     again, if the request is granted.
		/// </summary>
		/// <remarks>
		///     This event is used while dropping widgets onto the layout (from the widgets side panel):
		///     Once the user has made the drop, this event is fired and the widget has been added.
		/// </remarks>
		event Action<IWidgetPlugin> RequestAdd;
	}
}
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamanReader.Model.Database;
using GamanReader.ViewModel;

namespace GamanReader.View
{
	/// <summary>
	/// Interaction logic for TagTreePanel.xaml
	/// </summary>
	public partial class TagTreePanel : UserControl
	{
		public TagTreePanel()
		{
			InitializeComponent();
		}

		private void ItemDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			MangaInfo item = null;
			var window = (MainWindow)Window.GetWindow(this);
			Debug.Assert(window != null, nameof(window) + " != null");
			switch (preItem.DataContext)
			{
				case MangaInfo info:
					item = info;
					break;
				case AutoTag tag:
					item = tag.Item;
					break;
				case Alias alias:
					((MainViewModel)window.DataContext).Search($"alias:{alias.Name}");
					return;
			}
			if (item == null) return;
			window.LoadContainer(item);
		}

		public static TreeViewItem VisualUpwardSearch(DependencyObject source)
		{
			while (source != null && !(source is TreeViewItem)) source = System.Windows.Media.VisualTreeHelper.GetParent(source);
			return source as TreeViewItem;
		}

		internal void AddTag(MangaInfo item, string tag)
		{
			((TagTreeViewModel)DataContext).AddTag(item, tag);
		}
	}
}

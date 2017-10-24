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
			if (preItem.DataContext is MangaInfo) item = preItem.DataContext as MangaInfo;
			else if (preItem.DataContext is AutoTag) item = (preItem.DataContext as AutoTag).Item;
			if (item == null) return;
			var window = (MainWindow) Window.GetWindow(this);
			window.LoadContainer(item);
		}

		public static TreeViewItem VisualUpwardSearch(DependencyObject source)
		{
			while (source != null && !(source is TreeViewItem)) source = System.Windows.Media.VisualTreeHelper.GetParent(source);
			return source as TreeViewItem;
		}

		public void Refresh()
		{
			((TagTreeViewModel)DataContext).Refresh();
			foreach (TreeViewItem node in TreeStructure.Items)
			{
				node.IsExpanded = false;
			}
		}
	}
}

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
		
		private void MangaDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			MangaInfo item = preItem.DataContext as MangaInfo;
			if (item == null) return;
			var window = (MainWindow)Window.GetWindow(this);
			Debug.Assert(window != null, nameof(window) + " != null");
			window.LoadContainer(item);
	}

		private void TagDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			AutoTag tag = preItem.DataContext as AutoTag;
			if (tag == null) return;
			var window = (MainWindow)Window.GetWindow(this);
			Debug.Assert(window != null, nameof(window) + " != null");
			window.LoadContainer(tag.Item);
		}

	private void AliasDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			Alias alias = preItem.DataContext as Alias;
			if (alias == null) return;
			var window = (MainWindow)Window.GetWindow(this);
			Debug.Assert(window != null, nameof(window) + " != null");
			((MainViewModel)window.DataContext).Search($"alias:{alias.Name}");
	}

	public static TreeViewItem VisualUpwardSearch(DependencyObject source)
		{
			while (source != null && !(source is TreeViewItem)) source = System.Windows.Media.VisualTreeHelper.GetParent(source);
			return source as TreeViewItem;
		}

		internal void AddTag(MangaInfo item, string tag) => ((TagTreeViewModel)DataContext).AddTag(item, tag);

		internal void RemoveTag(MangaInfo item, string tag) => ((TagTreeViewModel)DataContext).RemoveTag(item, tag);
	}
}
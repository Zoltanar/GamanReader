using System.Diagnostics;
using System.Threading.Tasks;
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
		
		private async void MangaDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			MangaInfo item = preItem.DataContext as MangaInfo;
			await LoadContainer(item);
		}

		private async void TagDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			AutoTag tag = preItem.DataContext as AutoTag;
			await LoadContainer(tag?.Item);
	}

		private async Task LoadContainer(MangaInfo item)
		{
			if (item == null) return;
			var window = (MainWindow)Window.GetWindow(this);
			Debug.Assert(window != null, nameof(window) + " != null");
			await window.LoadContainer(item);
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
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamanReader.Model.Database;
using GamanReader.ViewModel;

namespace GamanReader.View
{
	public partial class TagTreePanel : UserControl
	{
		private bool _loaded;

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
			await window.LoadContainer(item, true);
		}

		private void AliasDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			if (!(preItem.DataContext is Alias alias)) return;
			var window = (MainWindow)Window.GetWindow(this);
			Debug.Assert(window != null, nameof(window) + " != null");
			((MainViewModel)window.DataContext).Search($"alias:{alias.Name}");
		}

		public static TreeViewItem VisualUpwardSearch(DependencyObject source)
		{
			while (source != null && !(source is TreeViewItem)) source = System.Windows.Media.VisualTreeHelper.GetParent(source);
			return source as TreeViewItem;
		}
		
		private void TagTreePanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			var window = (MainWindow)Window.GetWindow(this);
			(DataContext as TagTreeViewModel).Initialise(window.LoadDatabase);
			_loaded = true;
		}
	}
}
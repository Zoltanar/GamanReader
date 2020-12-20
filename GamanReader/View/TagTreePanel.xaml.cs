using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using JetBrains.Annotations;

namespace GamanReader.View
{
	public partial class TagTreePanel : UserControl
	{
		private bool _loaded;

		private MainWindow MainWindow => (MainWindow)Application.Current.MainWindow;

		public TagTreePanel() => InitializeComponent();

		private async void MangaDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			MangaInfo item = preItem.DataContext as MangaInfo;
			if (item == null) return;


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
			await MainWindow.LoadContainer(item, true);
		}

		private void AliasDoubleClicked(object sender, MouseButtonEventArgs e)
		{
			var preItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
			if (!(preItem.DataContext is Alias alias)) return;
			((MainViewModel)MainWindow.DataContext).Search($"alias:{alias.Name}");
		}

		public static TreeViewItem VisualUpwardSearch(DependencyObject source)
		{
			while (source != null && !(source is TreeViewItem)) source = System.Windows.Media.VisualTreeHelper.GetParent(source);
			return source as TreeViewItem;
		}

		private void TagTreePanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			((TagTreeViewModel)DataContext).Initialise(MainWindow.LoadDatabase);
			_loaded = true;
		}

		public class TreeViewTemplateSelector : DataTemplateSelector, INotifyPropertyChanged
		{
			private bool _showThumbnails;

			public bool ShowThumbnails
			{
				get => _showThumbnails;
				set
				{
					_showThumbnails = value;
					OnPropertyChanged();
				} 
			}

			public TreeViewTemplateSelector(bool showThumbnails) => ShowThumbnails = showThumbnails;

			public override DataTemplate SelectTemplate(object item, DependencyObject container)
			{
				var element = container as FrameworkElement;
				Debug.Assert(element != null, nameof(element) + " != null");
				switch (item)
				{
					case TagGroup _:
						return (DataTemplate) element.FindResource("TagGroupTemplate");
					case IMangaItem _:
						return (DataTemplate)Application.Current.FindResource("ItemWithImage");
					case AutoTag _:
						return (DataTemplate)element.FindResource("AutoTagTemplate");
					case Alias _:
						return (DataTemplate)element.FindResource("AliasTemplate");
					case FavoriteTag _:
						return (DataTemplate)element.FindResource("FavoriteTemplate");
					case null: return null;
					default: throw new ArgumentOutOfRangeException(item.GetType().ToString());
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;

			[NotifyPropertyChangedInvocator]
			protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
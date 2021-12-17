using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GamanReader.Model;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using JetBrains.Annotations;
using SevenZip;
using WpfAnimatedGif;
using static GamanReader.Model.StaticHelpers;
using Brushes = System.Windows.Media.Brushes;
using Container = GamanReader.Model.Container;
namespace GamanReader.View
{
	/// <summary>
	/// WPF application for reading manga with a dual-page view.
	/// </summary>
	public partial class MainWindow
	{
		private static readonly string PlayButtonText = (string)Application.Current.FindResource("PlayButtonString");
		private static readonly string StopButtonText = (string)Application.Current.FindResource("StopButtonString");
		private static readonly Regex NumberRegex = new(@"\d+", RegexOptions.Compiled);


		[NotNull] private readonly MainViewModel _mainModel;
		private bool _fullscreenOn;
		private bool _thumbnailViewOn;


		private string _pathToOpenOnInitialise;

		public MainWindow()
		{
			InitializeComponent();
			ToggleAnimationVisibility(false);
			_mainModel = (MainViewModel)DataContext;
			MainModel = this;
			LocalDatabase.TagViewModel = (TagTreeViewModel)TagPanel.DataContext;
			var firstPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Debug.Assert(firstPath != null, nameof(firstPath) + " != null");
			string path = Path.Combine(firstPath, "7z.dll");
			if (!File.Exists(path))
			{
				LogToFile("Failed to find 7z.dll in same folder as executable.");
				throw new FileNotFoundException("Failed to find 7z.dll in same folder as executable.", path);
			}
			//TODO try default 7zip install folder 
			SevenZipBase.SetLibraryPath(path);
			RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);
			var args = Environment.GetCommandLineArgs();
			if (args.Length > 1)
			{
				LoadDatabase = false;
				_pathToOpenOnInitialise = args[1];
			}
		}

		public bool LoadDatabase { get; private set; } = true;
		
		public void TagLinkClicked(object sender, RoutedEventArgs e)
		{
			var link = (Hyperlink)sender;
			var run = (Run)link.Inlines.FirstInline;
			var text = run.Text.Trim('[', '(', '{', ']', ')', '}');
			if (link.Tag is List<Hyperlink> list && list.Any())
			{
				var set = new HashSet<string>(list.Select(t =>
					((Run)t.Inlines.FirstInline).Text.Trim('[', '(', '{', ']', ')', '}')));
				set.UnionWith(new[] { text });
				_mainModel.SearchTags(set, true);
			}
			else _mainModel.Search($"alias:{text}", true);
		}

		public async Task LoadContainer(MangaInfo item, bool saveToDatabase) => await _mainModel.LoadContainer(item, saveToDatabase);

		private void GoToTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var text = ((TextBox)sender).Text;
			if (!int.TryParse(text, out var pageNumber))
			{
				//TODO ERROR
				return;
			}
			e.Handled = true;
			_mainModel.GoToPage(pageNumber);
		}

		private async void DropFile(object sender, DragEventArgs e)
		{
			string containerPath = ((DataObject)e.Data).GetFileDropList()[0];
			if (containerPath?.EndsWith(".lnk") ?? false) containerPath = GetPathFromShortcut(containerPath);
			if (string.IsNullOrWhiteSpace(containerPath)) return; //todo report error
			if (!File.Exists(containerPath) && !Directory.Exists(containerPath)) return; //todo report error
			await OpenContainerOrFile(containerPath);
		}

		private async Task OpenContainerOrFile(string containerPath)
		{
			const bool saveToDatabase = false;
			string openAtFile = null;
			if (File.Exists(containerPath) && Container.RecognizedExtensions.Contains(Path.GetExtension(containerPath)))
			{
				openAtFile = containerPath;
				containerPath = Directory.GetParent(containerPath).FullName;
			}
			var item = _mainModel.GetOrCreateMangaInfo(containerPath, saveToDatabase);
			await LoadContainer(item, saveToDatabase);
			if (_mainModel.MangaInfo == item && openAtFile != null) _mainModel.GoToPage(Array.IndexOf(_mainModel.ContainerModel.OrderedFileNames, openAtFile) + 1);
		}

		private void GoToTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			e.Handled = NumberRegex.IsMatch(e.Text);
		}

		private async void OpenRandom_Click(object sender, RoutedEventArgs e) => await _mainModel.OpenRandom(false);

		private async void OpenRandomFavorite_Click(object sender, RoutedEventArgs e) => await _mainModel.OpenRandom(true);

		private void AddTag(object sender, RoutedEventArgs e) => _mainModel.AddTag(TagText.Text);

		private void GoFullscreen(object sender, RoutedEventArgs e)
		{
			if (!_fullscreenOn)
			{
				WindowState = WindowState.Maximized;
				WindowStyle = WindowStyle.None;
			}
			else
			{
				WindowState = WindowState.Normal;
				WindowStyle = WindowStyle.SingleBorderWindow;
			}

			_fullscreenOn = !_fullscreenOn;
		}

		private void ReloadLibrary(object sender, RoutedEventArgs e) => _mainModel.ReloadLibraryInfo();

		private void TextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			_mainModel.Search(false);

		}

		private async void OpenItemFromListBox(object sender, MouseButtonEventArgs e)
		{
			if (e.OriginalSource is not DependencyObject source ||
					ItemsControl.ContainerFromElement((ItemsControl)sender, source) is not ListBoxItem lbItem ||
					lbItem.DataContext is not MangaInfo item) return;
			await _mainModel.LoadContainer(item);
		}

		private void DeleteItem(object sender, KeyEventArgs e)
		{
			var itemsControl = (ItemsControl)sender;
			if (e.Key != Key.Delete) return;
			if (!(e.OriginalSource is DependencyObject source)) return;
			if (!(ItemsControl.ContainerFromElement(itemsControl, source) is ListBoxItem lbItem)) return;
			if (!(lbItem.Content is MangaInfo item)) return;
			var deleted = _mainModel.RemoveItemFromDb(item, true, true);
			if (deleted) ((IList<MangaInfo>)itemsControl.ItemsSource).Remove(item);
		}

		private void IncreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			Grid.SetColumnSpan(TabsControl, 3);
			TabsControl.Background = Brushes.Transparent;
		}

		private void DecreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			Grid.SetColumnSpan(TabsControl, 1);
			TabsControl.Background = Brushes.White;
		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			// ReSharper disable once ConstantConditionalAccessQualifier
			if (_mainModel?.MangaInfo == null) return; //required because this is fired before initalization is completed#
			_mainModel.SliderPage = (int)e.NewValue;
		}

		private void Slider_OnDragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (_mainModel.MangaInfo == null) return;
			var value = (int)((Slider)e.Source).Value;
			if (value == _mainModel.CurrentIndex) return;
			_mainModel.GoToPage(value);
		}

		private void ImageBox_OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			((UIElement)sender).Focus();
			switch (e.ChangedButton)
			{
				case MouseButton.Left:
					_mainModel.GoForward(CtrlIsDown());
					break;
				case MouseButton.Right:
					_mainModel.GoBack(CtrlIsDown());
					break;
				case MouseButton.Middle:
					_mainModel.GoForward(true);
					break;
			}
		}

		/// <summary>
		/// Go forward on mousewheel inwards and back on outwards.
		/// </summary>
		private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (e.Delta < 0) _mainModel.GoForward(CtrlIsDown());
			else if (e.Delta > 0) _mainModel.GoBack(CtrlIsDown());
		}

		/// <summary>
		/// Direction-sensitive left and right arrow keys handler.
		/// </summary>
		private void HandleKeys(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Left:
					e.Handled = true;
					if (_mainModel.RtlIsChecked) _mainModel.GoForward(CtrlIsDown());
					else _mainModel.GoBack(CtrlIsDown());
					return;
				case Key.Right:
					e.Handled = true;
					if (_mainModel.RtlIsChecked) _mainModel.GoBack(CtrlIsDown());
					else _mainModel.GoForward(CtrlIsDown());
					return;
			}
		}

		private void ChangePageMode(object sender, RoutedEventArgs e) => _mainModel.ChangePageMode();

		private void ChangePageOrder(object sender, RoutedEventArgs e) => _mainModel.ChangePageOrder();

		private void GetLibraryAdditions(object sender, RoutedEventArgs e) => _mainModel.GetLibraryAdditions();

		private void CloseContainer(object sender, RoutedEventArgs e)
		{
			_mainModel.CloseContainer();
		}
		
		private void CreateAlias(object sender, RoutedEventArgs e)
		{
			var aliasWindow = new AliasWindow();
			aliasWindow.Show();
		}

		private void Window_Closing(object sender, CancelEventArgs e) => LocalDatabase.SaveChanges();

		private void ToggleThumbnails(bool showThumbs)
		{
			MangaInfo.ShowThumbnailForAll = showThumbs;
			var tagMangaItems = new List<IMangaItem>();
			GetVisibleMangaItems(tagMangaItems, TagPanel.TagTree);
			foreach (var item in _mainModel.LibraryItems.Concat(tagMangaItems))
			{
				if (showThumbs) MainViewModel.GetThumbInBackground(item);
				item.OnPropertyChanged(nameof(IMangaItem.ShowThumbnail));
			}
			_mainModel.OnPropertyChanged(nameof(_mainModel.LibraryItems));
			LibraryItems.ItemsPanel = (ItemsPanelTemplate) LibraryItems.Resources[showThumbs ? "WrapPanelTemplate" : "StackPanelTemplate"];
		}

		private void GetVisibleMangaItems(List<IMangaItem> visibleMangaItems, ItemsControl itemsControl)
		{
			int index = 0;
			var generator = itemsControl.ItemContainerGenerator;
			while (index < generator.Items.Count)
			{
				var item = generator.Items[index];
				index++;
				if (item is IMangaItem mangaItem) visibleMangaItems.Add(mangaItem);
				if (item is not TagGroup) continue;
				var node = generator.ContainerFromItem(item);
				if (node is TreeViewItem treeNode && treeNode.IsExpanded)
				{
					GetVisibleMangaItems(visibleMangaItems, treeNode);
				}
			}
		}

		private void ThumbnailViewToggle(object sender, RoutedEventArgs e)
		{
			_thumbnailViewOn = !_thumbnailViewOn;
			ToggleThumbnails(_thumbnailViewOn);
		}

		private void AutoPlayClick(object sender, RoutedEventArgs e)
		{
			var toggle = (ToggleButton)sender;
			if (toggle.IsChecked == true)
			{
				if (_mainModel.MangaInfo == null) return;
				AutoPlayButton.Content = StopButtonText;
				Dispatcher.Invoke(AutoPlayLoop);
			}
			else AutoPlayButton.Content = PlayButtonText;
		}

		private async Task AutoPlayLoop()
		{
			while (AutoPlayButton.IsChecked == true)
			{
				await Task.Delay(_mainModel.AutoPlaySpeed);
				if (AutoPlayButton.IsChecked != true) break;
				if (_mainModel.CurrentIndex + 2 > _mainModel.TotalFiles) _mainModel.GoToPage(1);
				else _mainModel.GoForward(false);
			}
		}

		private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			var tb = (TextBox)sender;
			var selected = tb.SelectedText;
			if (tb.Text.Length <= 0 || tb.CaretIndex <= 0 || selected.Length > 0) return;
			var subString = tb.Text.Substring(tb.CaretIndex);
			subString = new string(subString.TakeWhile(char.IsDigit).ToArray());
			if (subString.Length == 0) return;
			int pageIndex = int.Parse(subString);
			if (pageIndex <= _mainModel.TotalFiles) _mainModel.GoToPage(pageIndex);
		}

		private void SingleImageElement_OnTargetUpdated(object sender, DataTransferEventArgs e)
		{
			var controller = ImageBehavior.GetAnimationController(SingleImageElement);
			if (controller?.FrameCount > 1)
			{
				controller.CurrentFrameChanged += ControllerCurrentFrameChanged;
				SingleAnimationSlider.Value = 0;
				SingleAnimationSlider.Maximum = controller.FrameCount - 1;
				SingleAnimationMaxLabel.Content = $"{controller.FrameCount} ({controller.Duration.ToSeconds()}s)";
				ToggleAnimationVisibility(true);
			}
			else ToggleAnimationVisibility(false);
		}

		private void ControllerCurrentFrameChanged(object sender, EventArgs e)
		{
			var controller = (ImageAnimationController)sender;
			if (controller == null) return;
			SingleAnimationSlider.Value = controller.CurrentFrame;
			SingleAnimationActiveLabel.Content = controller.CurrentFrame + 1;
		}

		private void ToggleAnimationVisibility(bool visible)
		{
			PlayGifButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
			SingleAnimationSlider.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
			SingleAnimationActiveLabel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
			SingleAnimationMaxLabel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		}

		private void PlayGif_OnClicked(object sender, RoutedEventArgs e)
		{
			var toggle = (ToggleButton)sender;
			var controller = ImageBehavior.GetAnimationController(SingleImageElement);
			if (!(controller?.FrameCount > 1)) return;
			if (toggle.IsChecked == true)
			{
				controller.Pause();
				toggle.Content = PlayButtonText;
				ImageBehavior.SetAutoStart(SingleImageElement, false);
			}
			else
			{
				controller.Play();
				toggle.Content = StopButtonText;
				ImageBehavior.SetAutoStart(SingleImageElement, true);
			}
		}

		private void SingleAnimationSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var controller = ImageBehavior.GetAnimationController(SingleImageElement);
			if (!(controller?.FrameCount > 1)) return;
			var value = (int)e.NewValue;
			if (controller.CurrentFrame == value) return;
			controller.GotoFrame(value);
		}

		private void LibraryItemSelectorUpdated(object sender, DataTransferEventArgs e)
		{
			if (!_thumbnailViewOn) return;
			foreach (var item in _mainModel.LibraryItems) MainViewModel.GetThumbInBackground(item);
		}

		private bool _loaded;
		private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_loaded) return;
			if (!string.IsNullOrWhiteSpace(_pathToOpenOnInitialise)) await OpenContainerOrFile(_pathToOpenOnInitialise);
			_mainModel.Initialise(LoadDatabase);
			_pathToOpenOnInitialise = null;
			_loaded = true;
			var timeToLoad = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
			_mainModel.ReplyText = $"Time to load: {timeToLoad.TotalSeconds:#0.###} seconds.";
		}

		private void TagPageClick(object sender, RoutedEventArgs e)
		{
			_mainModel.MangaInfo.PageTags.Add(new PageTag { Index = _mainModel.CurrentPage });
			LocalDatabase.SaveChanges();
			_mainModel.OnPropertyChanged(nameof(_mainModel.TaggedPages));
			_mainModel.OnPropertyChanged(nameof(_mainModel.CurrentPageNotTagged));
		}

		private void PageTagSelected(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0 || !(e.AddedItems[0] is PageTag pageTag)) return;
			_mainModel.GoToPage(pageTag.Index);
		}

		private void DeletePageTag(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Delete) return;
			var grid = (DataGrid)sender;
			if (grid.SelectedItems.Count == 0 || !(grid.SelectedItems[0] is PageTag pageTag)) return;
			LocalDatabase.RemovePageTag(pageTag);
			_mainModel.OnPropertyChanged(nameof(_mainModel.CurrentPageNotTagged));
		}

		private void DeleteItemAndAddToDeleted(object sender, RoutedEventArgs e)
		{
			((App)Application.Current).DeleteItemAndAddToDeleted(sender, e);
		}

		private void FilterSearchResults(object sender, RoutedEventArgs e)
		{
			var view = CollectionViewSource.GetDefaultView(ViewModel.SearchResults);
			var searchBrowsed = SearchResultsBrowsedCheckBox?.IsChecked.GetValueOrDefault(true) ?? true;
			var searchDeleted = SearchResultsDeletedCheckBox?.IsChecked.GetValueOrDefault(true) ?? true;
			if (searchBrowsed && searchDeleted) view.Filter = null;
			view.Filter = o => o is IMangaItem mi && (searchBrowsed || mi.TimesBrowsed == 0) && (searchDeleted || !mi.IsDeleted);
		}

		private void SortSearchResults(object sender, RoutedEventArgs e)
		{
			var view = CollectionViewSource.GetDefaultView(ViewModel.SearchResults);
			var sortByName = SearchResultsByNameCheckBox?.IsChecked.GetValueOrDefault(false) ?? false;
			if (sortByName) view.SortDescriptions.Add(new SortDescription(nameof(IMangaItem.NoTagName), ListSortDirection.Ascending));
			else view.SortDescriptions.Clear();
		}

		private async void OpenRandomFromResults(object sender, RoutedEventArgs e)
		{
			var random = ViewModel.SearchResults.OfType<MangaInfo>().Where(i=>i.Exists()).ToList().SelectRandom();
			if (random != null) await ViewModel.LoadContainer(random);
		}
	}
}

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
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using JetBrains.Annotations;
using Microsoft.VisualBasic.FileIO;
using SevenZip;
using WpfAnimatedGif;
using static GamanReader.Model.StaticHelpers;

namespace GamanReader.View
{
	/// <summary>
	/// WPF application for reading manga with a dual-page view.
	/// </summary>
	public partial class MainWindow
	{
		public static readonly string PlayButtonText = (string)Application.Current.Resources["PlayButtonString"];
		public static readonly string StopButtonText = (string)Application.Current.Resources["StopButtonString"];

		[NotNull] private readonly MainViewModel _mainModel;
		private bool _fullscreenOn;

		public MainWindow()
		{
			InitializeComponent();
			ToggleAnimationVisibility(false);
			 _mainModel = (MainViewModel)DataContext;
			_mainModel.SingleImageElement = SingleImageElement;
			LocalDatabase.TagPanel = TagPanel;
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
				var item = _mainModel.GetOrCreateMangaInfo(args[1]);
				Task.Run(() => LoadContainer(item)).RunSynchronously();
			}

			_mainModel.RefreshTextBox += RefreshTextBox;
		}

		private void RefreshTextBox(MangaInfo item)
		{
			InfoBox.Inlines.Clear();
			if (item == null) return;
			foreach (var inline in item.GetTbInlines())
			{
				InfoBox.Inlines.Add(inline);
				if (inline is Hyperlink link) link.Click += TagLinkClicked;
			}
		}

		private void TagLinkClicked(object sender, RoutedEventArgs e)
		{
			var link = (Hyperlink)sender;
			var run = (Run)link.Inlines.FirstInline;
			var text = run.Text.Trim('[', '(', '{', ']', ')', '}');
			if (link.Tag is List<Hyperlink> list && list.Any())
			{
				var set = new HashSet<string>(list.Select(t =>
					((Run)t.Inlines.FirstInline).Text.Trim('[', '(', '{', ']', ')', '}')));
				set.UnionWith(new[] { text });
				_mainModel.SearchTags(set);
			}
			else _mainModel.Search($"alias:{text}");
		}
		
		public async Task LoadContainer(MangaInfo item) => await _mainModel.LoadContainer(item);

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
			var item = _mainModel.GetOrCreateMangaInfo(containerPath);
			await LoadContainer(item);

		}

		private void GoToTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
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
			_mainModel.Search();

		}

		private async void OpenItemFromListBox(object sender, MouseButtonEventArgs e)
		{
			if (!(e.OriginalSource is DependencyObject source)) return;
			if (!(ItemsControl.ContainerFromElement((ItemsControl)sender, source) is ListBoxItem lbItem)) return;
			if (!(lbItem.Content is MangaInfo item)) return;
			await _mainModel.LoadContainer(item);
		}

		private void DeleteItem(object sender, KeyEventArgs e)
		{
			var itemsControl = (ItemsControl)sender;
			if (e.Key != Key.Delete) return;
			if (!(e.OriginalSource is DependencyObject source)) return;
			if (!(ItemsControl.ContainerFromElement(itemsControl, source) is ListBoxItem lbItem)) return;
			if (!(lbItem.Content is MangaInfo item)) return;
			var deleted = _mainModel.DeleteItem(item);
			if (deleted) ((IList<MangaInfo>)itemsControl.ItemsSource).Remove(item);
		}

		private void IncreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			var control = (Control)sender;
			Grid.SetColumnSpan(control, 2);
			control.Background = Brushes.Transparent;
		}

		private void DecreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			var control = (Control)sender;
			Grid.SetColumnSpan(control, 1);
			control.Background = Brushes.White;
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

		private void GetLibraryAdditions(object sender, RoutedEventArgs e) => _mainModel.GetLibraryAdditions();

		private void CloseContainer(object sender, RoutedEventArgs e) => _mainModel.CloseContainer();

		private void CreateAlias(object sender, RoutedEventArgs e)
		{
			var aliasWindow = new AliasWindow();
			aliasWindow.Show();
		}

		private void DeleteContainer(object sender, RoutedEventArgs e) => _mainModel.DeleteCurrentItem();

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			LocalDatabase.SaveChanges();
		}

		private void DebugButtonClick(object sender, RoutedEventArgs e)
		{
#if DEBUG
			//await _mainModel.SetCantOpenOnNotBrowsed();
			//DeleteBlacklisted();
			DebugGetTotalSizeAndCalcCrc(sender, e);
			FindDuplicates();
			//FindBlacklistedSize();
#endif
		}

		private string[] ExcludedCrcStrings = {"Not Found", "0", "Deleted"};

		private void FindDuplicates()
		{
			var grouped = LocalDatabase.Items.Where(i => !ExcludedCrcStrings.Contains(i.CRC32))
				.GroupBy(c => c.CRC32).Where(g => g.Count() > 1).ToDictionary(g => g.Key, g => g.ToList());
			Console.WriteLine($"Found {grouped.Count} duplicates.");
			var duplicatesSize = 0D;
			foreach (var group in grouped)
			{
				foreach (var item in group.Value) Console.WriteLine($"\t[{group.Key}] {item.FilePath}");
				var size = group.Value.First().SizeMb;
				duplicatesSize += size * (group.Value.Count - 1);
			}

			Console.WriteLine($"Total size used by duplicates: {duplicatesSize:#0,000} MB");
		}

		private void FindBlacklistedSize()
		{
			var blacklisted = LocalDatabase.Items.Where(i =>
				i.UserTags.Any(t => t.Tag == BlacklistedTagString) && i.CRC32 != null && i.CRC32 != string.Empty &&
				i.CRC32 != "Not Found").ToList();
			Console.WriteLine($"Found {blacklisted.Count} blacklisted items.");
			var sizeSum = blacklisted.Sum(i => i.SizeMb);
			Console.WriteLine($"Total size used by blacklisted: {sizeSum:#0,000} MB");
		}

		private void DeleteBlacklisted()
		{
			var blacklisted = LocalDatabase.Items.Where(i =>
				i.UserTags.Any(t => t.Tag == BlacklistedTagString)).AsEnumerable().Where(a => a.Exists()).ToList();
			Console.WriteLine($"Found {blacklisted.Count} blacklisted items.");
			var sizeSum = blacklisted.Sum(i => i.SizeMb);
			Console.WriteLine($"Total size used by blacklisted: {sizeSum:#0,000} MB");
			var deleteCount = 0;
			foreach (var item in blacklisted)
			{
				item.CRC32 = "Deleted";
				if (item.IsFolder) FileSystem.DeleteDirectory(item.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
				else FileSystem.DeleteFile(item.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
				bool stillExists = item.IsFolder ? Directory.Exists(item.FilePath) : File.Exists(item.FilePath);
				if (!stillExists) deleteCount++;
			}
			Console.WriteLine($"Deleted {deleteCount} blacklisted items.");
		}

		private void DebugGetTotalSizeAndCalcCrc(object sender, RoutedEventArgs e)
		{
			var items = LocalDatabase.Items.AsEnumerable().Where(i => string.IsNullOrWhiteSpace(i.CRC32)).ToArray();
			Debug.WriteLine($"Calculating CRC for {items.Length} items...");
			var totalSizeMb = 0d;
			int step = (int)Math.Floor(items.Length * 0.25d);
			for (var index = 0; index < items.Length; index++)
			{
				if (index != 0 && index % 50 /*step*/ == 0)
				{
					Debug.WriteLine($"\tSize so far: {totalSizeMb:N0}");
					Debug.WriteLine($"\tCalculating CRC for item {index + 1}");
					LocalDatabase.SaveChanges();
				}
				var item = items[index];
				item.CalcCrc();
				if(Directory.Exists(item.FilePath)) totalSizeMb += item.SizeMb;
			}
			Debug.WriteLine($"\tTotal Size: {totalSizeMb:N0}");
			LocalDatabase.SaveChanges();
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
				//SetPlayPauseEnabled(_controller.IsPaused || _controller.IsComplete);
				ToggleAnimationVisibility(true);
			}
			else ToggleAnimationVisibility(false);
		}

		private void ControllerCurrentFrameChanged(object sender, EventArgs e)
		{
			var controller = (ImageAnimationController) sender;
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
			var toggle = (ToggleButton) sender;
			var controller = ImageBehavior.GetAnimationController(SingleImageElement);
			if (controller?.FrameCount > 1)
			{
				if (toggle.IsChecked == true)
				{
					controller.Pause();
					toggle.Content = PlayButtonText;
					ImageBehavior.SetAutoStart(SingleImageElement,false);
				}
				else
				{
					controller.Play();
					toggle.Content = StopButtonText;
					ImageBehavior.SetAutoStart(SingleImageElement, true);
				}
			}
		}
		
		private void SingleAnimationSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			var controller = ImageBehavior.GetAnimationController(SingleImageElement);
			if (controller?.FrameCount > 1)
			{
				var value = (int)e.NewValue;
				if (controller.CurrentFrame == value) return;
				controller.GotoFrame(value);
			}
		}
	}
}

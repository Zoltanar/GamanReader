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
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using JetBrains.Annotations;
using Microsoft.VisualBasic.FileIO;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SevenZip;
using static GamanReader.Model.StaticHelpers;

namespace GamanReader.View
{
	/// <summary>
	/// WPF application for reading manga with a dual-page view.
	/// </summary>
	public partial class MainWindow
	{
		[NotNull] private readonly MainViewModel _mainModel;
		private bool _fullscreenOn;

		public MainWindow()
		{
			InitializeComponent();
			_mainModel = (MainViewModel)DataContext;
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

		private async void LoadFolderByDialog(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			var item = _mainModel.GetOrCreateMangaInfo(folderPicker.FileName);
			await LoadContainer(item);
		}

		private async void LoadArchiveByDialog(object sender, RoutedEventArgs e)
		{
			var filePicker = new OpenFileDialog { Filter = "Archives|*.zip;*.rar" };
			bool? resultOk = filePicker.ShowDialog();
			if (resultOk != true) return;
			var item = _mainModel.GetOrCreateMangaInfo(filePicker.FileName);
			await LoadContainer(item);
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

		private void AddLibraryFolder_Click(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog
			{ IsFolderPicker = true, AllowNonFileSystemItems = true, Multiselect = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			foreach (var filename in folderPicker.FileNames) LocalDatabase.Libraries.Add(new LibraryFolder(filename));
			LocalDatabase.SaveChanges();
		}

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
			//DeleteBlacklisted();
			DebugGetTotalSizeAndCalcCrc(sender, e);
			//FindDuplicates();
			//FindBlacklistedSize();
#endif
		}

		private void FindDuplicates()
		{
			var grouped = LocalDatabase.Items.Where(i => i.CRC32 != null && i.CRC32 != String.Empty && i.CRC32 != "Not Found")
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
			var items = LocalDatabase.Items/*.Where(i => i.IsFolder && (i.CRC32 == null || i.CRC32 == string.Empty || i.CRC32 == "Not Found"))*/.ToArray();
			Debug.WriteLine($"Calculating CRC for {items.Length} items...");
			int step = (int)Math.Floor(items.Length * 0.25d);
			for (var index = 0; index < items.Length; index++)
			{
				if (index % 50 /*step*/ == 0)
				{
					Debug.WriteLine($"\tCalculating CRC for item {index + 1}");
					LocalDatabase.SaveChanges();
				}
				var item = items[index];
				item.CalcCrc();
			}

			LocalDatabase.SaveChanges();
		}

		private void DebugOld()
		{
			var nonExisting = LocalDatabase.Items.AsEnumerable().Where(i => !i.Exists()).ToList();
			var nonExistingWithReplicates = nonExisting
				.Select(item =>
					(item, Copies: LocalDatabase.Items.Where(i => i.Id != item.Id && i.Name == item.Name).ToArray()))
				.Where(p => p.Copies.Length != 0 && p.Copies.Any(c => c.Exists())).OrderByDescending(i => i.item.TimesBrowsed)
				.ToArray();
			//LogToFile($"Found {nonExisting.Count} files/folders that didn't exist, out of {LocalDatabase.Items.Count()} total items.");
			LogToFile(
				$"Found {nonExistingWithReplicates.Length} files/folders that didn't exist and have copies that exist, out of {LocalDatabase.Items.Count()} total items.");
			foreach (var (item, copies) in nonExistingWithReplicates)
			{
				var tags = item.UserTags.Count == 0
					? string.Empty
					: $"Tags: {string.Join("\t", item.UserTags.Select(t => t.Tag))}\t";
				LogToFile($"\tTimes Browsed:{item.TimesBrowsed}, {tags}{item.FilePath}");
				foreach (var copy in copies)
				{
					var otherTags = copy.UserTags.Count == 0
						? string.Empty
						: $"Tags: {string.Join("\t", copy.UserTags.Select(t => t.Tag))}\t";
					LogToFile($"\t\tCopy: Times Browsed:{copy.TimesBrowsed}, Exists:{copy.Exists()}, {otherTags}{copy.FilePath}");
				}

				var existingCopy = copies.Single(c => c.Exists());
				var nonExistingCopies = copies.Where(c => !c.Exists()).Concat(new[] { item }).ToList();
				var nonExistingCopiesString = string.Join(Environment.NewLine, nonExistingCopies.Select(i => i.SubPath));
				var result = /*MessageBoxResult.No*/ MessageBox.Show(
					$"Merge the following into {existingCopy.SubPath}?{Environment.NewLine}{nonExistingCopiesString}",
					"Confirm", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					var mergedTimesBrowsed = nonExistingCopies.Sum(c => c.TimesBrowsed) + existingCopy.TimesBrowsed;
					var mergedTags = nonExistingCopies.SelectMany(c => c.UserTags).Select(t => t.Tag).Distinct().ToList();
					foreach (var mergedTag in mergedTags)
					{
						if (existingCopy.UserTags.Any(ut => ut.Tag == mergedTag)) continue;
						existingCopy.UserTags.Add(new UserTag() { Tag = mergedTag });
					}

					existingCopy.TimesBrowsed = mergedTimesBrowsed;
					foreach (var neCopy in nonExistingCopies)
					{
						var success = LocalDatabase.Items.Remove(neCopy);
						if (success == null)
						{
						}
					}

					LocalDatabase.SaveChanges();
				}
			}
		}

		public void DebugSameFiles()
		{
			var groups = LocalDatabase.Items.AsEnumerable().GroupBy(i => (i.Name, i.IsFolder));
			foreach (var group in groups)
			{
				if (group.Count() == 1) continue;
				if (group.Key.IsFolder)
				{
					var sizeGroups = group.GroupBy(i => GetDirectorySize(new DirectoryInfo(i.FilePath))).ToList();
					if (sizeGroups.Count == 1)
					{
						LogToFile($"{group.Key.Name} found {sizeGroups.First().Count()} folders with same size");
						foreach (var file in sizeGroups.First()) LogToFile($"\t{file.FilePath}");
					}
					else
					{
						LogToFile($"{group.Key.Name} found {sizeGroups.Count} different sizes");
						foreach (var sizeGroup in sizeGroups)
						{
							foreach (var file in sizeGroup)
							{
								var sizeMb = sizeGroup.Key / 1024d / 1024d;
								LogToFile($"\t{sizeMb} Mb: {file.FilePath}");
							}
						}
					}
				}
				else
				{
					var sizeGroups = group.GroupBy(i =>
					{
						var fi = new FileInfo(i.FilePath);
						return fi.Exists ? fi.Length : -1;
					}).ToList();
					if (sizeGroups.Count == 1)
					{
						LogToFile($"{group.Key.Name} found {sizeGroups.First().Count()} files with same size");
						foreach (var file in sizeGroups.First()) LogToFile($"\t{file.FilePath}");
					}
					else
					{
						LogToFile($"{group.Key.Name} found {sizeGroups.Count} different sizes");
						foreach (var sizeGroup in sizeGroups)
						{
							foreach (var file in sizeGroup)
							{
								var sizeMb = sizeGroup.Key / 1024d / 1024d;
								LogToFile($"\t{sizeMb} Mb: {file.FilePath}");
							}
						}
					}
				}
			}
		}

		public static long GetDirectorySize(DirectoryInfo d)
		{
			if (!d.Exists) return -1;
			long size = 0;
			// Add file sizes.
			FileInfo[] fis = d.GetFiles();
			foreach (FileInfo fi in fis)
			{
				size += fi.Length;
			}

			// Add sub-directory sizes.
			DirectoryInfo[] dis = d.GetDirectories();
			foreach (DirectoryInfo di in dis)
			{
				size += GetDirectorySize(di);
			}

			return size;
		}

		private bool _autoPlaying;
		private void AutoPlayClick(object sender, RoutedEventArgs e)
		{
			if (!_autoPlaying)
			{
				if (_mainModel.MangaInfo == null) return;
				_autoPlaying = true;
				_mainModel.AutoPlayText = "⏹";
				{
					Task.Run(AutoPlayLoop);
				}
			}
			else
			{
				_autoPlaying = false;
				_mainModel.AutoPlayText = "▶";
			}
		}

		private async Task AutoPlayLoop()
		{
			while (_autoPlaying)
			{
				await Task.Delay(_mainModel.AutoPlaySpeed);
				if (!_autoPlaying) break;
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
	}
}

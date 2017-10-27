using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
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
		[JetBrains.Annotations.NotNull]
		private readonly MainViewModel _mainModel;

		public MainWindow()
		{
			InitializeComponent();
			_mainModel = (MainViewModel)DataContext;
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
				var item = GetOrCreateMangaInfo(args[1]);
				LoadContainer(item);
			}
			_mainModel.RefreshTextBox += RefreshTextBox;
		}

		private void RefreshTextBox(MangaInfo item)
		{
			if (item == null) return;
			InfoBox.Inlines.Clear();
			foreach (var inline in item.GetTbInlines())
			{
				InfoBox.Inlines.Add(inline);
				if (inline is Hyperlink link) link.Click += TagLinkClicked;
			}
		}

		private void TagLinkClicked(object sender, RoutedEventArgs e)
		{
			var text = ((Run) ((Hyperlink) sender).Inlines.FirstInline).Text.Trim('[', '(', '{', ']', ')', '}');
			_mainModel.Search($"tag:{text}");
		}


		private void LoadFolderByDialog(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			var item = GetOrCreateMangaInfo(folderPicker.FileName);
			LoadContainer(item);
		}

		private void LoadArchiveByDialog(object sender, RoutedEventArgs e)
		{
			var filePicker = new OpenFileDialog { Filter = "Archives|*.zip;*.rar" };
			bool? resultOk = filePicker.ShowDialog();
			if (resultOk != true) return;
			var item = GetOrCreateMangaInfo(filePicker.FileName);
			LoadContainer(item);
		}


		public void LoadContainer(MangaInfo item)
		{
			_mainModel.LoadContainer(item);
		}

		private void RecentCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0) return;
			if (e.AddedItems[0] != null && !e.AddedItems[0].ToString().Equals(""))
			{
				var item = e.AddedItems[0] as MangaInfo;
				LoadContainer(item); //todo list should have mangaInfo items
				//TODO clear from list if it doesnt exist
			}
		}



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

		private void DropFile(object sender, DragEventArgs e)
		{
			string containerPath = ((DataObject)e.Data).GetFileDropList()[0];
			if (containerPath?.EndsWith(".lnk") ?? false) containerPath = GetPathFromShortcut(containerPath);
			if (string.IsNullOrWhiteSpace(containerPath)) return; //todo report error
			var item = GetOrCreateMangaInfo(containerPath);
			LoadContainer(item);

		}

		private void GoToTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void OpenRandom_Click(object sender, RoutedEventArgs e)
		{
			_mainModel.OpenRandom();
		}

		private void SetLibraryFolder_Click(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			LocalDatabase.Libraries.Add(new LibraryFolder(folderPicker.FileName));
			LocalDatabase.SaveChanges();
		}

		private void AddTag(object sender, RoutedEventArgs e)
		{
			_mainModel.AddTag();
			TagPanel.Refresh();
		}

		private void SeeTagged_Click(object sender, RoutedEventArgs e)
		{
			TagPanel.Visibility = TagPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
		}

		private bool _fullscreenOn;
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

		private void ReloadLibrary(object sender, RoutedEventArgs e)
		{
			_mainModel.ReloadLibraryInfo();
		}

		private void TextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			_mainModel.Search();

		}
		
		private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!(e.OriginalSource is DependencyObject source)) return;
			if (!(ItemsControl.ContainerFromElement((ItemsControl) sender, source) is ListBoxItem lbItem)) return;
			if (lbItem.Content is MangaInfo item)
			{
				_mainModel.LoadContainer(item);
			}
	}

		private int _widthChange = 100;

		private void IncreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			LeftColumn.Width = new GridLength(LeftColumn.ActualWidth + _widthChange);
		}

		private void DecreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			LeftColumn.Width = new GridLength(LeftColumn.ActualWidth - _widthChange);
		}
		
  }
}

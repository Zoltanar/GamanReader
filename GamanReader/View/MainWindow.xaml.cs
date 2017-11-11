using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using JetBrains.Annotations;
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
		[NotNull]
		private readonly MainViewModel _mainModel;
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
			var text = ((Run)((Hyperlink)sender).Inlines.FirstInline).Text.Trim('[', '(', '{', ']', ')', '}');
			_mainModel.Search($"tag:{text}");
		}

		private void LoadFolderByDialog(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			var item = _mainModel.GetOrCreateMangaInfo(folderPicker.FileName);
			LoadContainer(item);
		}

		private void LoadArchiveByDialog(object sender, RoutedEventArgs e)
		{
			var filePicker = new OpenFileDialog { Filter = "Archives|*.zip;*.rar" };
			bool? resultOk = filePicker.ShowDialog();
			if (resultOk != true) return;
			var item = _mainModel.GetOrCreateMangaInfo(filePicker.FileName);
			LoadContainer(item);
		}

		public void LoadContainer(MangaInfo item)
		{
			_mainModel.LoadContainer(item);
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
			if (!File.Exists(containerPath) && !Directory.Exists(containerPath)) return; //todo report error
			var item = _mainModel.GetOrCreateMangaInfo(containerPath);
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

		private void AddLibraryFolder_Click(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true, Multiselect = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			foreach (var filename in folderPicker.FileNames) LocalDatabase.Libraries.Add(new LibraryFolder(filename));
			LocalDatabase.SaveChanges();
			_mainModel.DisplayedPanel = MainViewModel.DisplayPanel.Libraries;
		}

		private void AddTag(object sender, RoutedEventArgs e)
		{
			_mainModel.AddTag(TagText.Text);
		}

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

		private void OpenItemFromListBox(object sender, MouseButtonEventArgs e)
		{
			if (!(e.OriginalSource is DependencyObject source)) return;
			if (!(ItemsControl.ContainerFromElement((ItemsControl)sender, source) is ListBoxItem lbItem)) return;
			if (lbItem.Content is MangaInfo item)
			{
				_mainModel.LoadContainer(item);
			}
		}

		private void DeleteItem(object sender, KeyEventArgs e)
		{
			var itemsControl = (ItemsControl)sender;
			if (e.Key != Key.Delete) return; if (!(e.OriginalSource is DependencyObject source)) return;
			if (!(ItemsControl.ContainerFromElement(itemsControl, source) is ListBoxItem lbItem)) return;
			if (!(lbItem.Content is MangaInfo item)) return;
			var deleted = _mainModel.DeleteItem(item);
			if (deleted) ((IList<MangaInfo>)itemsControl.ItemsSource).Remove(item);
		}

		private readonly int _widthChange = 250;

		private void IncreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			LeftColumn.Width = new GridLength(LeftColumn.ActualWidth + _widthChange);
		}

		private void DecreaseWidthOnMouseEnter(object sender, MouseEventArgs e)
		{
			LeftColumn.Width = new GridLength(LeftColumn.ActualWidth - _widthChange);
		}

		private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			// ReSharper disable once ConstantConditionalAccessQualifier
			if (_mainModel?.MangaInfo == null) return; //required because this is fired before initalization is completed
			var value = (int)e.NewValue;
			IndexPopup.Child = new TextBlock(new Run($"Value: {value}")) { Foreground = Brushes.Blue, Background = Brushes.Violet };
			IndexPopup.IsOpen = true;
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
					if (_mainModel.RtlIsChecked) _mainModel.GoForward(CtrlIsDown());
					else _mainModel.GoBack(CtrlIsDown());
					return;
				case Key.Right:
					if (_mainModel.RtlIsChecked) _mainModel.GoBack(CtrlIsDown());
					else _mainModel.GoForward(CtrlIsDown());
					return;
			}
		}

		private void ChangePageMode(object sender, RoutedEventArgs e)
		{
			_mainModel.ChangePageMode();
		}

		private void GetLibraryAdditions(object sender, RoutedEventArgs e)
		{
			_mainModel.GetLibraryAdditions();
		}

		private void CloseContainer(object sender, RoutedEventArgs e)
		{
			_mainModel.CloseContainer();
		}

		private void CreateAlias(object sender, RoutedEventArgs e)
		{
			var aliasWindow = new AliasWindow();
			aliasWindow.Show();
		}
	}
}

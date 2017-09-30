using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GamanReader.Model;
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
			Directory.CreateDirectory(StoredDataFolder);
			Directory.CreateDirectory(TempFolder);
			foreach (var file in Directory.GetFiles(TempFolder))
			{
				File.Delete(file);
			}
			//
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
			if (args.Length > 1) _mainModel.LoadFolder(args[1]);
		}
		

		private void LoadFolderByDialog(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			_mainModel.LoadFolder(folderPicker.FileName);
		}

		private void LoadArchiveByDialog(object sender, RoutedEventArgs e)
		{
			var folderPicker = new OpenFileDialog { Filter = "Archives|*.zip;*.rar" };
			bool? resultOk = folderPicker.ShowDialog();
			if (resultOk != true) return;
			_mainModel.LoadArchive(folderPicker.FileName);
		}


		internal void LoadContainer(string containerPath)
		{
			if (Directory.Exists(containerPath)) _mainModel.LoadFolder(containerPath);
			else if (File.Exists(containerPath)) _mainModel.LoadArchive(containerPath);
			else _mainModel.ReplyText = "Container doesn't exist.";
		}

		private void RecentCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0) return;
			if (e.AddedItems[0] != null && !e.AddedItems[0].ToString().Equals(""))
			{
				var containerPath = e.AddedItems[0].ToString();
				LoadContainer(containerPath);
				//TODO clear from list if it doesnt exist
			}
		}
		
		private void GoToTextBox_KeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var text = ((TextBox) sender).Text;
			if (!int.TryParse(text, out var pageNumber))
			{
				//TODO ERROR
				return;
			}
			e.Handled = true;
			_mainModel.GoToIndex(pageNumber);
		}

		internal void ChangeTitle(string text)
		{
			Title = $"{text} - {ProgramName}";
		}

		private void DropFile(object sender, DragEventArgs e)
		{
			string containerPath = ((DataObject)e.Data).GetFileDropList()[0];
			if (containerPath == null) return;
			LoadContainer(containerPath);

		}

		private void GoToTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void OpenRandom_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Settings.LibraryFolder))
			{
				_mainModel.ReplyText = "Library Folder is not set";
				return;
			}
			var fileOrFolder = RandomFile.GetRandomFileOrFolder(Settings.LibraryFolder, out bool isFolder, out string error);
			if (fileOrFolder == null)
			{
				_mainModel.ReplyText = error;
				return;
			}
			if (isFolder) _mainModel.LoadFolder(fileOrFolder);
			else _mainModel.LoadArchive(fileOrFolder);
		}

		private void SetLibraryFolder_Click(object sender, RoutedEventArgs e)
		{
			var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
			var result = folderPicker.ShowDialog();
			if (result != CommonFileDialogResult.Ok) return;
			Settings.LibraryFolder = folderPicker.FileName;
		}

		private void AddTag(object sender, RoutedEventArgs e)
		{
			_mainModel.AddTag();
		}

		private void SeeTagged_Click(object sender, RoutedEventArgs e)
		{
			TagPanel.Visibility = TagPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
			var parentVis = ((DockPanel) TagPanel.Parent).Visibility;
			Debug.WriteLine($"TagPanel.Visibility={TagPanel.Visibility}, Parent.Visibility={parentVis}");
		}
	}
}

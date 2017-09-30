﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using GamanReader.Model;
using SevenZip;
using WpfAnimatedGif;
using Container = GamanReader.Model.Container;
using Image = System.Windows.Controls.Image;

namespace GamanReader.ViewModel
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public int CurrentIndex
		{
			get => _containerModel.CurrentIndex;
			set => _containerModel.CurrentIndex = value;
		}

		public int TotalFiles => _containerModel.TotalFiles;

		public MainViewModel()
		{
			_mainWindow = Application.Current.MainWindow as View.MainWindow;
			RtlIsChecked = false;
			DualPageIsChecked = false;
		}

		internal void GoBack(bool moveOne)
		{
			var moveNumber = moveOne ? 1 : PageSize;
			CurrentIndex = Math.Max(-1, CurrentIndex - moveNumber);
			PopulateBoxes();
		}

		public void PopulateBoxes()
		{
			ImageBox imagebox1;
			if (PageSize == 1) imagebox1 = ImageBox.Single;
			else imagebox1 = RtlIsChecked ? ImageBox.Right : ImageBox.Left;
			PopulateBox(imagebox1, CurrentIndex);
			if (PageSize == 1) return;
			var imagebox2 = RtlIsChecked ? ImageBox.Left : ImageBox.Right;
			PopulateBox(imagebox2, CurrentIndex + 1 > TotalFiles - 1 ? -1 : CurrentIndex + 1);
			GoToIndexText = (CurrentIndex+1).ToString();
			IndexLabelText = $"/{TotalFiles}";
		}

		public void GoForward(bool moveOne)
		{
			var moveNumber = moveOne ? 1 : PageSize;
			CurrentIndex = Math.Min(TotalFiles - 1, CurrentIndex + moveNumber);
			PopulateBoxes();
		}

		#region Properties
		private readonly View.MainWindow _mainWindow;
		private Container _containerModel;
		private string _rtlToggleText;
		private string _pageSizeToggleText;
		private string _rightLabelText;
		private string _leftLabelText;
		private string _tagText;
		private string _replyText;
		private string _indexLabelText;
		private string _goToIndexText;
		private bool _rtlIsChecked;
		private bool _dualPageIsChecked;
		private readonly RecentItemList<string> _recentFiles = new RecentItemList<string>(Settings.RecentListSize, Settings.RecentFolders);

		public string ReplyText { get => _replyText; set { _replyText = value; OnPropertyChanged("ReplyText"); } }
		public string TagText { get => _tagText; set { _tagText = value; OnPropertyChanged("TagText"); } }
		public string LeftLabelText { get => _leftLabelText; set { _leftLabelText = value; OnPropertyChanged("LeftLabelText"); } }
		public string RightLabelText { get => _rightLabelText; set { _rightLabelText = value; OnPropertyChanged("RightLabelText"); } }
		public string RtlToggleText { get => _rtlToggleText; set { _rtlToggleText = value; OnPropertyChanged("RtlToggleText"); } }
		public string PageSizeToggleText { get => _pageSizeToggleText; set { _pageSizeToggleText = value; OnPropertyChanged("PageSizeToggleText"); } }
		public string IndexLabelText { get => _indexLabelText; set { _indexLabelText = value; OnPropertyChanged("IndexLabelText"); } }
		public string GoToIndexText { get => _goToIndexText; set { _goToIndexText = value; OnPropertyChanged("GoToIndexText"); } }
		public bool RtlIsChecked
		{
			get => _rtlIsChecked;
			set
			{
				_rtlIsChecked = value;
				RtlToggleText = value ? "◀ Right-to-Left ◀" : "▶ Left-to-Right ▶";
				OnPropertyChanged("RtlIsChecked");
				if (_containerModel == null) return;
				PopulateBox(ImageBox.Single, -1);
				PopulateBox(ImageBox.Left, -1);
				PopulateBox(ImageBox.Right, -1);
				GoToIndex(CurrentIndex);
			}
		}
		public bool DualPageIsChecked
		{
			get => _dualPageIsChecked;
			set
			{
				_dualPageIsChecked = value;
				PageSize = _dualPageIsChecked ? 2 : 1;
				PageSizeToggleText = value ? "Dual Page View" : "Single Page View";
				OnPropertyChanged("DualPageIsChecked");
				if (_containerModel == null) return;
				PopulateBox(ImageBox.Single, -1);
				PopulateBox(ImageBox.Left, -1);
				PopulateBox(ImageBox.Right, -1);
				GoToIndex(CurrentIndex);
			}
		}
		public ObservableCollection<string> RecentItems => _recentFiles.Items;

		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public int PageSize { get; private set; }

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		internal void PopulateBox(ImageBox imagebox, int index)
		{
			var image = GetImage(imagebox);
			var filename = _containerModel.GetFile(index);
			if (filename == null)
			{
				image.Source = null;
				SetLabelText(imagebox, "");
				return;
			}
			SetLabelText(imagebox, $"({index + 1}) {Path.GetFileName(filename)}");
			if (Path.GetExtension(filename).Equals(".gif"))
				ImageBehavior.SetAnimatedSource(image, new BitmapImage(new Uri(filename)));
			else
			{
				ImageBehavior.SetAnimatedSource(image, null);
				image.Source = new BitmapImage(new Uri(filename));
			}
		}

		private Image GetImage(ImageBox boxForGetImage)
		{
			switch (boxForGetImage)
			{
				case ImageBox.Single: return _mainWindow.SingleImageBox;
				case ImageBox.Left: return _mainWindow.LeftImageBox;
				case ImageBox.Right: return _mainWindow.RightImageBox;
				default: throw new Exception($"Unexpected ImageBox value! ({boxForGetImage})");
			}
		}

		private void SetLabelText(ImageBox boxForLabelText, string text)
		{
			switch (boxForLabelText)
			{
				case ImageBox.Single:
					if (RtlIsChecked) RightLabelText = text;
					else LeftLabelText = text;
					return;
				case ImageBox.Left:
					LeftLabelText = text;
					return;
				case ImageBox.Right:
					RightLabelText = text;
					return;
				default: throw new Exception($"Unexpected ImageBox value! ({boxForLabelText})");
			}
		}

		internal void GoToPage(int pageNumber)
		{
			if (_containerModel == null) return;
			if (pageNumber < 0 || pageNumber > TotalFiles)
			{
				IndexLabelText = "Page out of range.";
				return;
			}
			GoToIndex(pageNumber - 1);
		}

		private void GoToIndex(int index)
		{
			CurrentIndex = index;
			PopulateBoxes();
		}

		#region Load Container

		internal void LoadArchive(string archivePath)
		{
			if (!File.Exists(archivePath))
			{
				ReplyText = "Archive doesn't exist.";
				return;
			}
			SevenZipExtractor zipFile = new SevenZipExtractor(archivePath);
			_containerModel = new ArchiveContainer(archivePath, zipFile.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af => af.FileName));
			LoadContainer(archivePath);
		}

		internal void LoadFolder(string folderName)
		{
			if (!Directory.Exists(folderName))
			{
				ReplyText = "Folder doesn't exist.";
				return;
			}
			var files = Directory.GetFiles(folderName).ToList();
			files.RemoveAll(i => Path.GetFileName(i) == "desktop.ini");
			var folders = Directory.GetDirectories(folderName);
			if (files.Count + folders.Length == 0)
			{
				ReplyText = "Folder is empty.";
				return;
			}
			if (folders.Any())
			{
				var result = MessageBox.Show("Include sub-folders?", "Include Folders", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					files.AddRange(Directory.GetFiles(folderName, "*", SearchOption.AllDirectories));
				}
			}
			_containerModel = new FolderContainer(folderName, files);
			LoadContainer(folderName);
		}

		private void LoadContainer(string containerName)
		{
			GoToIndex(0);
			ReplyText = _containerModel.TotalFiles + " images.";
			_mainWindow.ChangeTitle(Path.GetFileName(containerName));
			_recentFiles.Add(_containerModel.ContainerPath);
			GoToIndexText = (_containerModel.CurrentIndex + 1).ToString();
			IndexLabelText = $"/{_containerModel.TotalFiles}";
			Settings.Save(_recentFiles.Items);
		}

		internal void AddTag()
		{
			if (_containerModel == null) return;
			StaticHelpers.AddTag(_containerModel.ContainerPath, _containerModel.IsFolder, TagText);
			//todo write reply
			TagText = "";
		}

		#endregion

		public enum ImageBox
		{
			Single = 0,
			Left = 1,
			Right = 2
		}
	}
}

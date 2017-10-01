using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using GamanReader.Model;
using JetBrains.Annotations;
using SevenZip;
using static GamanReader.Model.StaticHelpers;
using Container = GamanReader.Model.Container;

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
			RtlIsChecked = true;
			DualPageIsChecked = true;
			Directory.CreateDirectory(StoredDataFolder);
			Directory.CreateDirectory(TempFolder);
		}

		internal void GoBack(bool moveOne)
		{
			if (_containerModel == null) return;
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
			GoToIndexText = (CurrentIndex + 1).ToString();
			IndexLabelText = $"/{TotalFiles}";
		}

		public void GoForward(bool moveOne)
		{
			if (_containerModel == null) return;
			var moveNumber = moveOne ? 1 : PageSize;
			CurrentIndex = Math.Min(TotalFiles - 1, CurrentIndex + moveNumber);
			PopulateBoxes();
		}

		#region Properties
		private Container _containerModel;
		private string _rtlToggleText;
		private string _pageSizeToggleText;
		private string _rightLabelText;
		private string _leftLabelText;
		private string _tagText;
		private string _titleText;
		private string _replyText;
		private string _indexLabelText;
		private string _goToIndexText;
		private bool _rtlIsChecked;
		private bool _dualPageIsChecked;
		private readonly RecentItemList<string> _recentFiles = new RecentItemList<string>(Settings.RecentListSize, Settings.RecentFolders);
		private string _singleImageSource;
		private string _leftImageSource;
		private string _rightImageSource;
		private MangaInfo _mangaInfo;

		public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }
		public string ReplyText { get => _replyText; set { _replyText = value; OnPropertyChanged(); } }
		public string TagText { get => _tagText; set { _tagText = value; OnPropertyChanged(); } }
		public string LeftLabelText { get => _leftLabelText; set { _leftLabelText = value; OnPropertyChanged(); } }
		public string RightLabelText { get => _rightLabelText; set { _rightLabelText = value; OnPropertyChanged(); } }
		public string RtlToggleText { get => _rtlToggleText; set { _rtlToggleText = value; OnPropertyChanged(); } }
		public string PageSizeToggleText { get => _pageSizeToggleText; set { _pageSizeToggleText = value; OnPropertyChanged(); } }
		public string IndexLabelText { get => _indexLabelText; set { _indexLabelText = value; OnPropertyChanged(); } }
		public string GoToIndexText { get => _goToIndexText; set { _goToIndexText = value; OnPropertyChanged(); } }
		public bool RtlIsChecked
		{
			get => _rtlIsChecked;
			set
			{
				_rtlIsChecked = value;
				RtlToggleText = value ? "◀ Right-to-Left ◀" : "▶ Left-to-Right ▶";
				OnPropertyChanged();
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
				OnPropertyChanged();
				if (_containerModel == null) return;
				PopulateBox(ImageBox.Single, -1);
				PopulateBox(ImageBox.Left, -1);
				PopulateBox(ImageBox.Right, -1);
				GoToIndex(CurrentIndex);
			}
		}
		public string SingleImageSource
		{
			get => _singleImageSource;
			set
			{
				_singleImageSource = value;
				OnPropertyChanged();
			}
		}
		public string LeftImageSource
		{
			get => _leftImageSource;
			set
			{
				_leftImageSource = value;
				OnPropertyChanged();
			}
		}
		public string RightImageSource
		{
			get => _rightImageSource;
			set
			{
				_rightImageSource = value;
				OnPropertyChanged();
			}
		}
		public MangaInfo MangaInfo
		{
			get => _mangaInfo;
			set
			{
				_mangaInfo = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<string> RecentItems => _recentFiles.Items;

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public int PageSize { get; private set; }
		

		#endregion

		internal void PopulateBox(ImageBox imagebox, int index)
		{
			var filename = _containerModel.GetFile(index);
			SetImage(imagebox, filename);
			SetLabelText(imagebox, filename == null ? "" : $"({index + 1}) {Path.GetFileName(filename)}");
		}

		private void SetImage(ImageBox boxForImage, string file)
		{
			switch (boxForImage)
			{
				case ImageBox.Single:
					SingleImageSource = file;
					return;
				case ImageBox.Left:
					LeftImageSource = file;
					return;
				case ImageBox.Right:
					RightImageSource = file;
					return;
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

		public void LoadArchive(string archivePath)
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

		public void LoadFolder(string folderName)
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
			MangaInfo = MangaInfo.FromFilename(Path.GetFileNameWithoutExtension(containerName));
			GoToIndex(0);
			ReplyText = _containerModel.TotalFiles + " images.";
			TitleText = $"{Path.GetFileName(containerName)} - {ProgramName}";
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

		public void OpenRandom()
		{
			if (string.IsNullOrEmpty(Settings.LibraryFolder))
			{
				ReplyText = "Library Folder is not set";
				return;
			}
			var fileOrFolder = RandomFile.GetRandomFileOrFolder(Settings.LibraryFolder, out bool isFolder, out string error);
			if (fileOrFolder == null)
			{
				ReplyText = error;
				return;
			}
			if (isFolder) LoadFolder(fileOrFolder);
			else LoadArchive(fileOrFolder);
		}

#if TEST
		public Container GetContainer()
		{
		return _containerModel;
		}
#endif
	}
}

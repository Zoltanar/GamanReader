using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
		public MainViewModel()
		{
			TitleText = "GamanReader";
			RtlIsChecked = true;
			DualPageIsChecked = true;
			Directory.CreateDirectory(StoredDataFolder);
			try { Directory.Delete(TempFolder, true); }
			catch (IOException) { /*This can fail if folder is open or files are used by other applications.*/ }
			Directory.CreateDirectory(TempFolder);
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
		private int _pageSize;
		private string _searchText;

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
				PopulateBoxes();
			}
		}
		public bool DualPageIsChecked
		{
			get => _dualPageIsChecked;
			set
			{
				_dualPageIsChecked = value;
				_pageSize = _dualPageIsChecked ? 2 : 1;
				PageSizeToggleText = value ? "Dual Page View" : "Single Page View";
				OnPropertyChanged();
				SingleImageSource = null;
				LeftImageSource = null;
				RightImageSource = null;
				PopulateBoxes();
			}
		}
		public string SingleImageSource { get => _singleImageSource; set { _singleImageSource = value; OnPropertyChanged(); } }
		public string LeftImageSource { get => _leftImageSource; set { _leftImageSource = value; OnPropertyChanged(); } }
		public string RightImageSource { get => _rightImageSource; set { _rightImageSource = value; OnPropertyChanged(); } }
		public MangaInfo MangaInfo { get => _mangaInfo; set { _mangaInfo = value; OnPropertyChanged(); } }
		public int CurrentIndex { get => _containerModel.CurrentIndex; set => _containerModel.CurrentIndex = value; }
		public ObservableCollection<string> RecentItems => _recentFiles.Items;
		public int TotalFiles => _containerModel.TotalFiles;

		public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }

		public BindingList<MangaInfo> SearchResults { get; set; } = new BindingList<MangaInfo>();

		#endregion

		private void PopulateBox(ImageBox imagebox, int index)
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
			}
		}

		public void GoToPage(int pageNumber)
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

		public void AddTag()
		{
			if (_containerModel == null) return;
			StaticHelpers.AddTag(_containerModel.ContainerPath, _containerModel.IsFolder, TagText);
			//todo write reply
			TagText = "";
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

		public void GoBack(bool moveOne)
		{
			if (_containerModel == null) return;
			var moveNumber = moveOne ? 1 : _pageSize;
			CurrentIndex = Math.Max(-1, CurrentIndex - moveNumber);
			PopulateBoxes();
		}

		public void GoForward(bool moveOne)
		{
			if (_containerModel == null) return;
			var moveNumber = moveOne ? 1 : _pageSize;
			CurrentIndex = Math.Min(TotalFiles - 1, CurrentIndex + moveNumber);
			PopulateBoxes();
		}

		private void PopulateBoxes()
		{
			if (_containerModel == null) return;
			ImageBox imagebox1;
			if (_pageSize == 1) imagebox1 = ImageBox.Single;
			else imagebox1 = RtlIsChecked ? ImageBox.Right : ImageBox.Left;
			PopulateBox(imagebox1, CurrentIndex);
			if (_pageSize == 1) return;
			var imagebox2 = RtlIsChecked ? ImageBox.Left : ImageBox.Right;
			PopulateBox(imagebox2, CurrentIndex + 1 > TotalFiles - 1 ? -1 : CurrentIndex + 1);
			GoToIndexText = (CurrentIndex + 1).ToString();
			IndexLabelText = $"/{TotalFiles}";
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
			var files = zipFile.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af => af.FileName).ToArray();
			_containerModel = new ArchiveContainer(archivePath, files);
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
			if (_containerModel.TotalFiles == 0)
			{
				ReplyText = "No files found in container";
				_containerModel = null;
				return;
			}
			MangaInfo = new MangaInfo(containerName);
			GoToIndex(0);
			ReplyText = _containerModel.TotalFiles + " images.";
			TitleText = $"{Path.GetFileName(containerName)} - {ProgramName}";
			_recentFiles.Add(_containerModel.ContainerPath);
			GoToIndexText = (_containerModel.CurrentIndex + 1).ToString();
			IndexLabelText = $"/{_containerModel.TotalFiles}";
			Settings.Save(_recentFiles.Items);
		}

		#endregion

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private enum ImageBox
		{
			Single = 0,
			Left = 1,
			Right = 2
		}


		public async void ReloadLibraryInfo()
		{
			await Task.Run(() =>
			{
				LocalDatabase.Information.RemoveRange(LocalDatabase.Information);
				LocalDatabase.SaveChanges();
				var files = Directory.GetFiles(Settings.LibraryFolder);
				var folders = Directory.GetDirectories(Settings.LibraryFolder);
				int count = 0;
				int total = files.Length + folders.Length;
				foreach (var container in files.Concat(folders))
				{
					count++;
					Console.Write($@"Processing item {count}/{total} - {Path.GetFileNameWithoutExtension(container)}".PadRight(200)); //todo report progress
					LocalDatabase.Information.Add(new MangaInfo(container));
				}
				Console.WriteLine();
				LocalDatabase.SaveChanges();
			});
		}

		public void Search()
		{
			SearchResults.Clear();
			var searchParts = SearchText.Split(':');
			var type = searchParts.Length == 2 ? searchParts[0] : "";
			var searchString = searchParts.Length == 2 ? searchParts[1] : SearchText;
			MangaInfo[] results;
			switch (type)
			{
				case "event":
					results = LocalDatabase.Information.Where(x => x.Event.ToLower().Equals(searchString.ToLower())).ToArray();
					break;
				case "group":
					results = LocalDatabase.Information.Where(x => x.Group.ToLower().Equals(searchString.ToLower())).ToArray();
					break;
				case "artist":
					results = LocalDatabase.Information.Where(x => x.Artist.ToLower().Equals(searchString.ToLower())).ToArray();
					break;
				case "parody":
					results = LocalDatabase.Information.Where(x => x.Parody.ToLower().Equals(searchString.ToLower())).ToArray();
					break;
				case "subber":
					results = LocalDatabase.Information.Where(x => x.Subber.ToLower().Equals(searchString.ToLower())).ToArray();
					break;
				case "":
					results = LocalDatabase.Information.Where(x => x.Title.ToLower().Contains(searchString.ToLower())).ToArray();
					break;
					default:
					throw new ArgumentException("Argument is invalid.");
			}
			SearchResults.AddRange(results);
		}

		internal void Search(string searchString)
		{
			SearchText = searchString;
			Search();
		}

		public void LoadFromMangaInfo(MangaInfo info)
		{
			var attributes = File.GetAttributes(info.FilePath);
			if (attributes.HasFlag(FileAttributes.Directory)) LoadFolder(info.FilePath);
			else LoadArchive(info.FilePath);
		}
	}
}

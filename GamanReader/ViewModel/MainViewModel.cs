﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using GamanReader.Model;
using GamanReader.Model.Database;
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
			if (!LocalDatabase.Libraries.Any())
			{
				ReplyText = "No Library Folders are set.";
				return;
			}
			var item = LocalDatabase.Information.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
			if (item == null)
			{
				ReplyText = "No items found.";
				return;
			}
			LoadContainer(item);
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

		private void LoadArchive(MangaInfo item)
		{
			if (!File.Exists(item.FilePath))
			{
				ReplyText = "Archive doesn't exist.";
				return;
			}
				SevenZipExtractor zipFile = new SevenZipExtractor(item.FilePath);
				var files = zipFile.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af => af.FileName).ToArray();
			_containerModel?.Dispose();
			_containerModel = new ArchiveContainer(item.FilePath, files);
		}

		private void LoadFolder(MangaInfo item)
		{
			if (!Directory.Exists(item.FilePath))
			{
				ReplyText = "Folder doesn't exist.";
				return;
			}
			var files = Directory.GetFiles(item.FilePath).ToList();
			files.RemoveAll(i => Path.GetFileName(i) == "desktop.ini");
			var folders = Directory.GetDirectories(item.FilePath);
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
					files.AddRange(Directory.GetFiles(item.FilePath, "*", SearchOption.AllDirectories));
				}
			}
			_containerModel?.Dispose();
			_containerModel = new FolderContainer(item.FilePath, files);
		}

		public void LoadContainer(MangaInfo item)
		{
			try
			{
			if(item.IsFolder) LoadFolder(item);
			else LoadArchive(item);
			}
			catch(Exception ex)
			{
				ReplyText = $"Failed - {ex.Message}";
				return;
			}
			if (_containerModel == null) return;
			if (_containerModel.TotalFiles == 0)
			{
				ReplyText = "No files found in container";
				_containerModel = null;
				return;
			}
			MangaInfo = item;
			GoToIndex(0);
			ReplyText = _containerModel.TotalFiles + " images.";
			TitleText = $"{Path.GetFileName(item.FilePath)} - {ProgramName}";
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
				foreach(var library in LocalDatabase.Libraries) ReloadLibraryInfo(library);
				LocalDatabase.SaveChanges();
			});
		}

		private void ReloadLibraryInfo(LibraryFolder library)
		{
			if (string.IsNullOrWhiteSpace(library.Path)) return;
			var files = Directory.GetFiles(library.Path);
			var folders = Directory.GetDirectories(library.Path);
			int count = 0;
			int total = files.Length + folders.Length;
			foreach (var file in files)
			{
				count++;
				//Console.Write($@"Processing item {count}/{total} - {Path.GetFileNameWithoutExtension(file)}".PadRight(200)); //todo report progress
				if (!FileIsSupported(file)) continue;
				LocalDatabase.Information.Add(MangaInfo.Create(file,library,false));
			}
			foreach (var folder in folders)
			{
				count++;
				//Console.Write($@"Processing item {count}/{total} - {Path.GetFileNameWithoutExtension(folder)}".PadRight(200)); //todo report progress
				LocalDatabase.Information.Add(MangaInfo.Create(folder, library, true));
			}
			ReplyText = "Finished Reloading Library";
		}

		private bool FileIsSupported(string file)
		{
			return RecognizedContainers.Contains(Path.GetExtension(file));
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
	}
}

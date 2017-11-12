using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using GamanReader.Model;
using GamanReader.Model.Database;
using JetBrains.Annotations;
using Microsoft.VisualBasic.FileIO;
using static GamanReader.Model.StaticHelpers;
using Container = GamanReader.Model.Container;
using SearchOption = System.IO.SearchOption;

namespace GamanReader.ViewModel
{
	public class MainViewModel : INotifyPropertyChanged
	{
		public MainViewModel()
		{
			TitleText = "GamanReader";
			RtlIsChecked = true;
			PageView = PageMode.Auto;
			Directory.CreateDirectory(StoredDataFolder);
			try { Directory.Delete(TempFolder, true); }
			catch (IOException) { /*This can fail if folder is open or files are used by other applications.*/ }
			Directory.CreateDirectory(TempFolder);
		}

		#region Properties
		private Container _containerModel;
		private string _rtlToggleText;
		private string _pageModeText;
		private string _rightLabelText;
		private string _leftLabelText;
		private string _titleText;
		private string _replyText;
		private string _indexLabelText;
		private bool _rtlIsChecked;
		private PageMode _pageView;
		private readonly RecentItemList<MangaInfo> _lastOpened = new RecentItemList<MangaInfo>(25, LocalDatabase.GetLastOpened(25));
		private readonly RecentItemList<MangaInfo> _lastAdded = new RecentItemList<MangaInfo>(25, LocalDatabase.GetLastAdded(25));
		private string _singleImageSource;
		private string _leftImageSource;
		private string _rightImageSource;
		private MangaInfo _mangaInfo;
		private string _searchText;

		public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }
		public string ReplyText { get => _replyText; set { _replyText = value; OnPropertyChanged(); } }
		public string LeftLabelText { get => _leftLabelText; set { _leftLabelText = value; OnPropertyChanged(); } }
		public string RightLabelText { get => _rightLabelText; set { _rightLabelText = value; OnPropertyChanged(); } }
		public string RtlToggleText { get => _rtlToggleText; set { _rtlToggleText = value; OnPropertyChanged(); } }
		public string PageModeText { get => _pageModeText; set { _pageModeText = value; OnPropertyChanged(); } }
		public string IndexLabelText { get => _indexLabelText; set { _indexLabelText = value; OnPropertyChanged(); } }
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
		private PageMode PageView
		{
			get => _pageView;
			set
			{
				_pageView = value;
				switch (value)
				{
					case PageMode.Single:
						PageModeText = "Single Page View";
						break;
					case PageMode.Dual:
						PageModeText = "Dual Page View";
						break;
					case PageMode.Auto:
						PageModeText = "Auto Page View";
						break;
				}
				SingleImageSource = null;
				LeftImageSource = null;
				RightImageSource = null;
				PopulateBoxes();
			}
		}
		public object SingleImageSource
		{
			get => string.IsNullOrWhiteSpace(_singleImageSource) ? DependencyProperty.UnsetValue : _singleImageSource;
			set
			{
				_singleImageSource = (string)value;
				OnPropertyChanged();
			}
		}
		public object LeftImageSource
		{
			get => string.IsNullOrWhiteSpace(_leftImageSource) ? DependencyProperty.UnsetValue : _leftImageSource;
			set
			{
				_leftImageSource = (string)value;
				OnPropertyChanged();
			}
		}
		public object RightImageSource
		{
			get => string.IsNullOrWhiteSpace(_rightImageSource) ? DependencyProperty.UnsetValue : _rightImageSource;
			set
			{
				_rightImageSource = (string)value;
				OnPropertyChanged();
			}
		}
		public MangaInfo MangaInfo
		{
			get => _mangaInfo;
			set
			{
				_mangaInfo = value;
				RefreshTextBox?.Invoke(_mangaInfo);
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsFavorite));
				OnPropertyChanged(nameof(IsBlacklist));
			}
		}

		public int CurrentIndex
		{
			get => _containerModel?.CurrentIndex ?? -1;
			set
			{
				_containerModel.CurrentIndex = value;
				OnPropertyChanged(nameof(CurrentPage));
			}
		}

		public int CurrentPage
		{
			get => (_containerModel?.CurrentIndex ?? -1) + 1;
		}
		public BindingList<MangaInfo> LastOpenedItems => _lastOpened.Items;
		public BindingList<MangaInfo> LastAddedItems => _lastAdded.Items;
		public int TotalFiles => _containerModel?.TotalFiles ?? 1;
		private bool DisplayingOnePage => RightImageSource == DependencyProperty.UnsetValue || LeftImageSource == DependencyProperty.UnsetValue;
		public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }
		public BindingList<MangaInfo> SearchResults { get; set; } = new BindingList<MangaInfo>();
		public delegate void MyEventAction(MangaInfo item);
		public event MyEventAction RefreshTextBox;
		#endregion

		private void PopulateBox(ImageBox imagebox, int index)
		{
			var filename = _containerModel?.GetFile(index);
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

		public void AddTag(string tag)
		{
			if (_containerModel == null) return;
			LocalDatabase.AddTag(MangaInfo, tag);
			DisplayedPanel = DisplayPanel.Tags;
		}

		public void OpenRandom()
		{
			if (!LocalDatabase.Libraries.Any())
			{
				ReplyText = "No Library Folders are set.";
				return;
			}
			var item = NoBlacklisted ? LocalDatabase.Items.Where(x => !x.UserTags.Any(y => y.Tag.ToLower().Equals("blacklisted"))).OrderBy(z => Guid.NewGuid()).FirstOrDefault()
				: LocalDatabase.Items.OrderBy(x => Guid.NewGuid()).FirstOrDefault();
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
			var moveNumber = moveOne ? 1 : 2;
			var targetIndex = Math.Max(-1, CurrentIndex - moveNumber);
			if (targetIndex == CurrentIndex) return;
			CurrentIndex = targetIndex;
			PopulateBoxes();
		}

		public void GoForward(bool moveOne)
		{
			if (_containerModel == null) return;
			var moveNumber = moveOne ? 1 : 2;
			if (moveNumber == 2 && DisplayingOnePage) moveNumber = 1;
			CurrentIndex = Math.Min(TotalFiles - 1, CurrentIndex + moveNumber);
			PopulateBoxes();
		}

		private void PopulateBoxes()
		{
			if (_containerModel == null) return;
			switch (PageView)
			{
				case PageMode.Auto:
					PopulateBoxesAuto();
					return;
				case PageMode.Single:
					PopulateBox(ImageBox.Single, CurrentIndex);
					break;
				case PageMode.Dual:
					var imagebox1 = RtlIsChecked ? ImageBox.Right : ImageBox.Left;
					var imagebox2 = RtlIsChecked ? ImageBox.Left : ImageBox.Right;
					PopulateBox(imagebox1, CurrentIndex);
					PopulateBox(imagebox2, CurrentIndex + 1 > TotalFiles - 1 ? -1 : CurrentIndex + 1);
					break;
			}
			IndexLabelText = $"/{TotalFiles}";
		}

		public void CloseContainer()
		{
			_containerModel?.Dispose();
			_containerModel = null;
			MangaInfo = null;
			PopulateBox(ImageBox.Single, -1);
			PopulateBox(ImageBox.Left, -1);
			PopulateBox(ImageBox.Right, -1);
			IndexLabelText = "Closed";
			TitleText = "Gaman Reader";
		}

		private void PopulateBoxesAuto()
		{
			//first, get first image
			ImageBox imagebox2 = RtlIsChecked ? ImageBox.Left : ImageBox.Right;
			if (CurrentIndex == -1)
			{
				PopulateBox(RtlIsChecked ? ImageBox.Right : ImageBox.Left, CurrentIndex);
			}
			else
			{
				var filename = _containerModel.GetFile(CurrentIndex);
				var img1 = new Bitmap(filename);
				var ratio1 = img1.PhysicalDimension.Width / img1.PhysicalDimension.Height;
				ImageBox imagebox1 = ratio1 > 1 ? ImageBox.Single : (RtlIsChecked ? ImageBox.Right : ImageBox.Left);
				PopulateBox(imagebox1, CurrentIndex);
				if (ratio1 > 1)
				{
					LeftImageSource = null;
					SetLabelText(imagebox2, "(none)");
					RightImageSource = null;
					return;
				}
			}
			SingleImageSource = null;
			if (CurrentIndex + 1 >= TotalFiles)
			{
				PopulateBox(imagebox2, -1);
				return;
			}
			var filename2 = _containerModel.GetFile(CurrentIndex + 1);
			var img2 = new Bitmap(filename2);
			var ratio2 = img2.PhysicalDimension.Width / img2.PhysicalDimension.Height;
			PopulateBox(imagebox2, ratio2 > 1 ? -1 : CurrentIndex + 1);
		}

		#region Load Container

		private void LoadArchive(MangaInfo item)
		{
			if (!File.Exists(item.FilePath))
			{
				ReplyText = "Archive doesn't exist.";
				return;
			}
			_containerModel?.Dispose();
			switch (Path.GetExtension(item.FilePath))
			{
				case ".zip":
					_containerModel = new ZipContainer(item.FilePath, () => OnPropertyChanged(nameof(Extracted)));
#pragma warning disable 4014
					Task.Run(() => ((ZipContainer)_containerModel).ExtractAllAsync());
#pragma warning restore 4014
					break;
				case ".rar":
					_containerModel = new RarContainer(item.FilePath, () => OnPropertyChanged(nameof(Extracted)));
					break;
			}
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
			if (MangaInfo == item) return;
			try
			{
				if (item.IsFolder) LoadFolder(item);
				else LoadArchive(item);
			}
			catch (Exception ex)
			{
				ReplyText = $"Failed - {ex.Message}";
				return;
			}
			if (_containerModel == null) return;
			if (_containerModel.TotalFiles == 0)
			{
				ReplyText = "No files found in container";
				MangaInfo = item;
				_containerModel = null;
				return;
			}
			MangaInfo = item;
			OnPropertyChanged(nameof(TotalFiles));
			GoToIndex(0);
			ReplyText = _containerModel.TotalFiles + " images.";
			TitleText = $"{Path.GetFileName(item.FilePath)} - {ProgramName}";
			_lastOpened.Add(item);
			item.LastOpened = DateTime.Now;
			LocalDatabase.SaveChanges();
			IndexLabelText = $"/{_containerModel.TotalFiles}";
		}

		#endregion

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public async void ReloadLibraryInfo()
		{
			if (!UserIsSure()) return;
			await Task.Run(() =>
			{
				LocalDatabase.Items.RemoveRange(LocalDatabase.Items);
				LocalDatabase.SaveChanges();
				foreach (var library in LocalDatabase.Libraries) ReloadLibraryInfo(library);
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
				if (count % 10 == 0) ReplyText = $@"Processing item {count}/{total}...";
				if (!FileIsSupported(file)) continue;
				LocalDatabase.Items.Add(MangaInfo.Create(file, library, false));
			}
			foreach (var folder in folders)
			{
				count++;
				if (count % 10 == 0) ReplyText = $@"Processing item {count}/{total}...";
				LocalDatabase.Items.Add(MangaInfo.Create(folder, library, true));
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
			var searchString = (searchParts.Length == 2 ? searchParts[1] : SearchText).Trim();
			MangaInfo[] results;
			switch (type)
			{
				case "alias":
					var alias = LocalDatabase.Aliases.FirstOrDefault(x => x.Name.ToLower().Equals(searchString.ToLower()));
					if (alias == null) goto case "tag";
					else results = LocalDatabase.AutoTags.Where(x => alias.Tags.Contains(x.Tag.ToLower())).Select(y => y.Item).ToArray();
					break;
				case "tag":
					results = LocalDatabase.AutoTags.Where(x => x.Tag.ToLower().Equals(searchString.ToLower())).Select(y => y.Item).ToArray();
					break;
				case "":
					results = LocalDatabase.Items.Where(x => x.Name.ToLower().Contains(searchString.ToLower())).ToArray();
					break;
				default:
					throw new ArgumentException("Argument is invalid.");
			}
			if (NoBlacklisted) results = results.Where(x => x.UserTags.All(y => y.Tag.ToLower() != "blacklisted")).ToArray();
			SearchResults.AddRange(results.Distinct().OrderBy(x => x.Name));
			DisplayedPanel = DisplayPanel.Search;
		}

		public bool NoBlacklisted { get; set; } = true;

		public bool IsFavorite
		{
			get => MangaInfo?.UserTags.Any(x => x.Tag.ToLower().Equals("favorite")) ?? false;
			set
			{
				if (value == MangaInfo.UserTags.Any(x => x.Tag.ToLower().Equals("favorite"))) return;
				if (value) LocalDatabase.AddTag(MangaInfo, "favorite");
				else LocalDatabase.RemoveTag(MangaInfo, "favorite");
				OnPropertyChanged();
			}
		}

		public bool IsBlacklist
		{
			get => MangaInfo?.UserTags.Any(x => x.Tag.ToLower().Equals("blacklisted")) ?? false;
			set
			{
				if (value == MangaInfo.UserTags.Any(x => x.Tag.ToLower().Equals("blacklisted"))) return;
				if (value) LocalDatabase.AddTag(MangaInfo, "blacklisted");
				else LocalDatabase.RemoveTag(MangaInfo, "blacklisted");
				OnPropertyChanged();
			}
		}

		private DisplayPanel _displayedPanel = DisplayPanel.Opened;

		public object DisplayedPanel
		{
			get => (int)_displayedPanel;
			set
			{
				switch (value)
				{
					case int iVal:
						_displayedPanel = (DisplayPanel)iVal;
						break;
					case DisplayPanel eVal:
						_displayedPanel = eVal;
						break;
					default:
						throw new ArgumentException("Must pass object of type DisplayPanel or int");
				}
				OnPropertyChanged();
			}
		}

		public int Extracted => _containerModel?.Extracted ?? 0;

		public enum DisplayPanel { Search, Tags, Libraries, Added, Opened }

		internal void Search(string searchString)
		{
			SearchText = searchString;
			Search();
		}



		public void ChangePageMode()
		{
			switch (PageView)
			{
				case PageMode.Single:
					PageView = PageMode.Dual;
					break;
				case PageMode.Dual:
					PageView = PageMode.Auto;
					break;
				case PageMode.Auto:
					PageView = PageMode.Single;
					break;
			}
		}

		private enum ImageBox { Single, Left, Right }
		private enum PageMode { Single, Dual, Auto }

		public bool DeleteCurrentItem()
		{
			return DeleteItem(MangaInfo);
		}

		/// <summary>
		/// Returns true if deleted or false if not
		/// </summary>
		/// <param name="item"></param>
		/// <returns>true if deleted</returns>
		public bool DeleteItem(MangaInfo item)
		{
			if (!UserIsSure()) return false;
			if(item.Id == MangaInfo.Id) CloseContainer();
			if (item.IsFolder) FileSystem.DeleteDirectory(item.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
			else FileSystem.DeleteFile(item.FilePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
			bool stillExists = item.IsFolder ? Directory.Exists(item.FilePath) : File.Exists(item.FilePath);
			if (!stillExists)
			{
				LocalDatabase.Items.Remove(item);
				LocalDatabase.SaveChanges();
			}
			return !stillExists;
		}

		public async void GetLibraryAdditions()
		{
			int additions = 0;
			await Task.Run(() =>
			{
				LocalDatabase.Items.Load();
				foreach (var library in LocalDatabase.Libraries) additions += GetLibraryAdditions(library);
				LocalDatabase.SaveChanges();
			});
			_lastAdded.Items.SetRange(LocalDatabase.GetLastAdded(25));
			ReplyText = $"Finished getting new additions ({additions})";
			DisplayedPanel = DisplayPanel.Added;
		}

		private int GetLibraryAdditions(LibraryFolder library)
		{
			if (string.IsNullOrWhiteSpace(library.Path)) return 0;
			int additions = 0;
			var files = Directory.GetFiles(library.Path);
			var folders = Directory.GetDirectories(library.Path);
			var presentFiles = LocalDatabase.Items.Local.Where(x => !x.IsFolder).Select(x => x.FilePath).ToArray();
			var presentFolders = LocalDatabase.Items.Local.Where(x => x.IsFolder).Select(x => x.FilePath).ToArray();
			int count = 0;
			int total = files.Length + folders.Length;
			foreach (var file in files)
			{
				count++;
				if (count % 10 == 0) ReplyText = $@"Processing item {count}/{total}...";
				if (!FileIsSupported(file)) continue;
				if (presentFiles.Contains(file)) continue;
				additions++;
				LocalDatabase.Items.Add(MangaInfo.Create(file, library, false));
			}
			foreach (var folder in folders)
			{
				count++;
				if (count % 10 == 0) ReplyText = $@"Processing item {count}/{total}...";
				if (presentFolders.Contains(folder)) continue;
				additions++;
				LocalDatabase.Items.Add(MangaInfo.Create(folder, library, true));
			}
			return additions;
		}

		public MangaInfo GetOrCreateMangaInfo(string containerPath)
		{
			return LocalDatabase.GetOrCreateMangaInfo(containerPath, _lastAdded);
		}
	}
}

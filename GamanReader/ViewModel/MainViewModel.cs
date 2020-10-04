using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
			LocalDatabase.DeletedItems.Load();
			var stats = new List<string>
			{
				$"Total Items: {LocalDatabase.Items.Count()}",
				$"Favorited Items: {LocalDatabase.Items.Count(IsFavorited())}",
				$"Blacklisted Items: {LocalDatabase.Items.Count(IsBlacklisted())}",
				$"Browsed Items: {LocalDatabase.Items.Count(i => i.TimesBrowsed > 0)}",
				$"Not Browsed Items: {LocalDatabase.Items.Count(i => i.TimesBrowsed == 0)}",
				$"Not Browsed Items (<50 Pages): {LocalDatabase.Items.Count(i =>i.TimesBrowsed == 0 && i.FileCount < 50)}",
				$"Not Browsed Items (50+ Pages): {LocalDatabase.Items.Count(i => i.TimesBrowsed == 0 && i.FileCount >= 50)}",
				$"Deleted Items: {DeletedItems.Count()}"
			};
			Stats = stats;
		}

		public const int MaxListItems = 100;
		#region Properties
		public Container ContainerModel { get; private set; }
		private string _rtlToggleText;
		private string _pageModeText;
		private string _rightLabelText;
		private string _leftLabelText;
		private string _titleText;
		private string _replyText;
		private string _indexLabelText;
		private bool _rtlIsChecked;
		private PageMode _pageView;
		private readonly RecentItemList<MangaInfo> _lastOpened = new RecentItemList<MangaInfo>(MaxListItems, LocalDatabase.GetLastOpened(MaxListItems));
		private readonly RecentItemList<MangaInfo> _lastAdded = new RecentItemList<MangaInfo>(MaxListItems, LocalDatabase.GetLastAdded(MaxListItems));
		private readonly RecentItemList<MangaInfo> _mostBrowsed = new RecentItemList<MangaInfo>(MaxListItems, LocalDatabase.GetMostBrowsed(MaxListItems));
		public readonly RecentItemList<MangaInfo> _notBrowsed = new RecentItemList<MangaInfo>(MaxListItems, LocalDatabase.GetNotBrowsed(MaxListItems));
		private object _singleImageSource;
		private object _leftImageSource;
		private object _rightImageSource;
		private MangaInfo _mangaInfo;
		private string _searchText;

		public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }
		public string ReplyText { get => _replyText; set { _replyText = value; OnPropertyChanged(); } }
		public string LeftLabelText
		{
			get => _leftLabelText;
			set
			{
				if (_leftLabelText == value) return;
				_leftLabelText = value;
				OnPropertyChanged();
			}
		}
		public string RightLabelText
		{
			get => _rightLabelText;
			set
			{
				if (_rightLabelText == value) return;
				_rightLabelText = value;
				OnPropertyChanged();
			}
		}
		public string RtlToggleText { get => _rtlToggleText; set { _rtlToggleText = value; OnPropertyChanged(); } }
		public string PageModeText { get => _pageModeText; set { _pageModeText = value; OnPropertyChanged(); } }
		public string IndexLabelText { get => _indexLabelText; set { _indexLabelText = value; OnPropertyChanged(); } }
		public string[] Pages => ContainerModel?.FileNames.Select((f, i) => $"[{i:000}] {f}").ToArray() ?? Array.Empty<string>();
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
			get => _singleImageSource ?? DependencyProperty.UnsetValue;
			set
			{
				if (_singleImageSource == value) return;
				_singleImageSource = value;
				if (value != null)
				{
					LeftImageSource = null;
					RightImageSource = null;
				}
				OnPropertyChanged();
			}
		}
		public object LeftImageSource
		{
			get => _leftImageSource ?? DependencyProperty.UnsetValue;
			set
			{
				if (_leftImageSource == value) return;
				if(value != null) SingleImageSource = null;
				_leftImageSource = value;
				OnPropertyChanged();
			}
		}
		public object RightImageSource
		{
			get => _rightImageSource ?? DependencyProperty.UnsetValue;
			set
			{
				if (_rightImageSource == value) return;
				if (value != null) SingleImageSource = null;
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
				RefreshTextBox?.Invoke(_mangaInfo);
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsFavorite));
				OnPropertyChanged(nameof(IsBlacklist));
				OnPropertyChanged(nameof(Pages));
			}
		}
		public int CurrentIndex
		{
			get => ContainerModel?.CurrentIndex ?? -1;
			set
			{
				ContainerModel.CurrentIndex = value;
				OnPropertyChanged(nameof(CurrentPage));
				SliderPage = value + 1;
			}
		}
		public int CurrentPage => (ContainerModel?.CurrentIndex ?? -1) + 1;
		public int TotalFiles => ContainerModel?.TotalFiles ?? 1;
		private bool DisplayingOnePage => RightImageSource == DependencyProperty.UnsetValue || LeftImageSource == DependencyProperty.UnsetValue;
		public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }
		public int AutoPlaySpeed { get; set; } = 1000;
		public ObservableCollection<MangaInfo> SearchResults { get; set; } = new ObservableCollection<MangaInfo>();

		public LibrarySelector[] LibraryItemSelectors { get; } = Enum.GetValues(typeof(LibrarySelector)).Cast<LibrarySelector>().ToArray();
		private LibrarySelector _libraryItemSelector = LibrarySelector.Added;
		public LibrarySelector LibraryItemSelector
		{
			get => _libraryItemSelector;
			set
			{
				_libraryItemSelector = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(LibraryItems));
				OnPropertyChanged(nameof(LibraryItemFormat));
				OnPropertyChanged(nameof(LibrarySearchVisibility));
			}
		}

		public Visibility LibrarySearchVisibility =>
			LibraryItemSelector == LibrarySelector.Search ? Visibility.Visible : Visibility.Collapsed;

		public ObservableCollection<MangaInfo> LibraryItems
		{
			get
			{
				return LibraryItemSelector switch
				{
					LibrarySelector.Added => _lastAdded.Items,
					LibrarySelector.Opened => _lastOpened.Items,
					LibrarySelector.Browsed => _mostBrowsed.Items,
					LibrarySelector.NotBrowsed => _notBrowsed.Items,
					LibrarySelector.Search => SearchResults,
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}
		public string LibraryItemFormat
		{
			get
			{
				return LibraryItemSelector switch
				{
					LibrarySelector.Added => string.Empty,
					LibrarySelector.Opened => string.Empty,
					LibrarySelector.Browsed => "T",
					LibrarySelector.NotBrowsed => "D",
					LibrarySelector.Search => "D",
					_ => throw new ArgumentOutOfRangeException()
				};
			}
		}

		public bool DebugOn
		{
			get
			{
#if DEBUG
				return true;
#else
				return false;
#endif
			}
		}

		public delegate void MyEventAction(MangaInfo item);
		public event MyEventAction RefreshTextBox;

		#endregion

		private void PopulateBox(ImageBox imageBox, int index)
		{
			var filename = ContainerModel.GetFile(index, out var displayName);
			var image = new BitmapImage(new Uri(filename));
			PopulateBox(imageBox, index, image, displayName);
		}

		private void PopulateEmpty(ImageBox imageBox)
		{
			PopulateBox(imageBox, -1, null, null);
		}

		private void PopulateBox(ImageBox imageBox, int index, BitmapImage image, string displayName)
		{
			SetImage(imageBox, image);
			SetLabelText(imageBox, string.IsNullOrWhiteSpace(displayName) ? string.Empty : $"({index + 1}) {displayName}");
		}

		private void SetImage(ImageBox boxForImage, ImageSource image)
		{
			switch (boxForImage)
			{
				case ImageBox.Single:
					SingleImageSource = image;
					return;
				case ImageBox.Left:
					LeftImageSource = image;
					return;
				case ImageBox.Right:
					RightImageSource = image;
					return;
				default:
					throw new ArgumentOutOfRangeException(nameof(boxForImage), boxForImage, null);
			}
		}

		private void SetLabelText(ImageBox boxForLabelText, string text)
		{
			switch (boxForLabelText)
			{
				case ImageBox.Single:
					if (RtlIsChecked)
					{
						RightLabelText = text;
						LeftLabelText = "(none)";
					}
					else
					{
						LeftLabelText = text;
						RightLabelText = "(none)";
					}

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
			if (ContainerModel == null) return;
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
			if (ContainerModel.Extracted < index + 1) Thread.Sleep(25);
			PopulateBoxes();
		}

		public void AddTag(string tag)
		{
			if (ContainerModel == null) return;
			LocalDatabase.AddTag(MangaInfo, tag);
			DisplayedPanel = DisplayPanel.Tags;
		}

		public async Task OpenRandom(bool favorites)
		{
			var item = (LocalDatabase.Items.AsEnumerable() ?? throw new InvalidOperationException("Collection was unexpectedly null."))
				.Where(x => !x.IsBlacklisted && (favorites ? x.IsFavorite : x.TimesBrowsed == 0))
				.OrderBy(z => Guid.NewGuid()).FirstOrDefault(x=>x.Exists());
			if (item == null)
			{
				ReplyText = "No items found.";
				return;
			}
			await LoadContainer(item);
		}

		public void GoBack(bool moveOne)
		{
			if (ContainerModel == null) return;
			int moveNumber;
			if (PageView == PageMode.Single || moveOne || ContainerModel.CurrentIndex < 2) moveNumber = 1;
			else
			{
				var bitmap1 = GetImage(ContainerModel.GetFile(CurrentIndex - 1, out _));
				var ratio1 = bitmap1.Width / bitmap1.Height;
				var bitmap2 = GetImage(ContainerModel.GetFile(CurrentIndex - 2, out _));
				var ratio2 = bitmap2.Width / bitmap2.Height;
				moveNumber = ratio1 > 1 || ratio2 > 1 ? 1 : 2;
			}

			var targetIndex = Math.Max(-1, CurrentIndex - moveNumber);
			if (targetIndex == CurrentIndex) return;
			CurrentIndex = targetIndex;
			PopulateBoxes();
		}

		public void GoForward(bool moveOne)
		{
			if (ContainerModel == null) return;
			var moveNumber = PageView == PageMode.Single || moveOne ? 1 : 2;
			if (moveNumber == 2 && DisplayingOnePage) moveNumber = 1;
			if (CurrentIndex + (DisplayingOnePage ? 0 : 1) == TotalFiles - 1 && !moveOne) return;
			CurrentIndex = Math.Min(TotalFiles - 1, CurrentIndex + moveNumber);
			PopulateBoxes();
		}

		private void PopulateBoxes()
		{
			if (ContainerModel == null) return;
			switch (PageView)
			{
				case PageMode.Auto:
					PopulateBoxesAuto();
					return;
				case PageMode.Single:
					PopulateBox(ImageBox.Single, CurrentIndex);
					break;
				case PageMode.Dual:
					var imageBox1 = RtlIsChecked ? ImageBox.Right : ImageBox.Left;
					var imageBox2 = RtlIsChecked ? ImageBox.Left : ImageBox.Right;
					PopulateBox(imageBox1, CurrentIndex);
					if (CurrentIndex + 1 > TotalFiles - 1) PopulateEmpty(imageBox2);
					else PopulateBox(imageBox2, CurrentIndex + 1);
					break;
				default: throw new ArgumentOutOfRangeException();
			}
			IndexLabelText = $"/{TotalFiles}";
		}

		public void CloseContainer()
		{
			ContainerModel?.Dispose();
			ContainerModel = null;
			MangaInfo = null;
			PopulateEmpty(ImageBox.Single);
			PopulateEmpty(ImageBox.Left);
			PopulateEmpty(ImageBox.Right);
			IndexLabelText = "Closed";
			TitleText = "Gaman Reader";
			RefreshTextBox?.Invoke(null);
		}

		private void PopulateBoxesAuto()
		{
			if (CurrentIndex == TotalFiles - 1)
			{
				PopulateLastImage();
				return;
			}
			ImageBox imageBox2;
			double ratio2;
			BitmapImage img2;
			string displayName2;
			try
			{
				//first, get first image
				imageBox2 = RtlIsChecked ? ImageBox.Left : ImageBox.Right;
				var filename2 = ContainerModel.GetFile(CurrentIndex + 1, out displayName2);
				img2 = GetImage(filename2);
				ratio2 = img2.Width / img2.Height;
				if (CurrentIndex == -1) PopulateEmpty(RtlIsChecked ? ImageBox.Right : ImageBox.Left);
				else
				{
					var filename = ContainerModel.GetFile(CurrentIndex, out var displayName);
					var img1 = GetImage(filename);
					var ratio1 = img1.Width / img1.Height;
					var imageBox1 = ratio1 > 1 || ratio2 > 1 ? ImageBox.Single : (RtlIsChecked ? ImageBox.Right : ImageBox.Left);
					PopulateBox(imageBox1, CurrentIndex, img1, displayName);
					if (ratio1 > 1 || ratio2 > 1)
					{
						LeftImageSource = null;
						SetLabelText(imageBox2, "(none)");
						RightImageSource = null;
						return;
					}
				}
			}
			catch (Exception ex)
			{
				LogToFile(nameof(PopulateBoxesAuto), ex);
				throw;
			}
			SingleImageSource = null;
			if (CurrentIndex + 1 >= TotalFiles)
			{
				PopulateEmpty(imageBox2);
				return;
			}
			if (ratio2 > 1) PopulateEmpty(imageBox2);
			else PopulateBox(imageBox2, CurrentIndex + 1, img2, displayName2);
		}

		private void PopulateLastImage()
		{
			var filename = ContainerModel.GetFile(CurrentIndex, out var displayName);
			var image = GetImage(filename);
			if (image.Width > image.Height)
			{
				PopulateBox(ImageBox.Single, CurrentIndex, image, displayName);
			}
			else
			{
				PopulateBox(RtlIsChecked ? ImageBox.Right : ImageBox.Left, CurrentIndex, image, displayName);
				if (RtlIsChecked) LeftImageSource = null;
				else RightImageSource = null;
				SetLabelText(RtlIsChecked ? ImageBox.Left : ImageBox.Right, "(none)");
			}
		}

		private static BitmapImage GetImage(string filename)
		{
			//support for other file types goes here
			return RunWithRetries(() => new BitmapImage(new Uri(filename)), ex => ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException, 5, 50);
		}

		#region Load Container

		private CancellationTokenSource _tokenSource;
		private Task _zipArchiveTask;

		private async Task LoadArchive(MangaInfo item)
		{
			if (!File.Exists(item.FilePath))
			{
				ReplyText = "Archive doesn't exist.";
				return;
			}
			ContainerModel?.Dispose();
			var extension = Path.GetExtension(item.FilePath);
			Action onPropertyChanged = () => OnPropertyChanged(nameof(Extracted));
			switch (extension)
			{
				case ".cbz":
				case ".zip":
					ContainerModel = new ZipContainer(item, onPropertyChanged);
					_tokenSource?.Cancel();
					_zipArchiveTask?.Wait();
					_tokenSource = new CancellationTokenSource();
#pragma warning disable 4014
					Task.Run(ZipExtractAllAsync, CancellationToken.None);
#pragma warning restore 4014
					break;
				case ".rar":
					ContainerModel = await Task.Run(() =>
					{
						var containerModel = new RarContainer(item, onPropertyChanged);
						return containerModel;
					});
					break;
				default:
					throw new InvalidOperationException($"Extension: '{extension}' is not supported.");
			}
		}

		private async Task ZipExtractAllAsync()
		{
			await ((ZipContainer)ContainerModel).ExtractAllAsync(_tokenSource.Token);
			_tokenSource = null;
			_zipArchiveTask = null;
		}

		private void LoadFolder(MangaInfo item)
		{
			if (!Directory.Exists(item.FilePath))
			{
				ReplyText = "Folder doesn't exist.";
				return;
			}
			var directoryInfo = new DirectoryInfo(item.FilePath);
			var files = MangaInfo.GetImageFiles(directoryInfo, SearchOption.TopDirectoryOnly);
			var folders = Directory.GetDirectories(item.FilePath);
			if (files.Length + folders.Length == 0)
			{
				ReplyText = "Folder is empty.";
				return;
			}
			if (folders.Any())
			{
				var result = MessageBox.Show("Include sub-folders?", "Include Folders", MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					files = MangaInfo.GetImageFiles(directoryInfo, SearchOption.AllDirectories);
				}
			}
			ContainerModel?.Dispose();
			ContainerModel = new FolderContainer(item, files.Select(f=>f.FullName));
		}

		public async Task LoadContainer(MangaInfo item, bool saveToDatabase = true)
		{
			if (MangaInfo == item) return;
			if (saveToDatabase && string.IsNullOrWhiteSpace(item.CRC32)) item.CalcCrc();
			var logEntry = $"Attempting to open {item.Name}";
			if ((item.IsFolder && !Directory.Exists(item.FilePath)) ||
					(!item.IsFolder && !File.Exists(item.FilePath)))
			{
				CloseContainer();
				var failedLog = $"Failed - File or folder did not exist: {item.FilePath}";
				ReplyText = failedLog;
				System.Diagnostics.Debug.WriteLine(failedLog);
				return;
			}
			ReplyText = logEntry;
			System.Diagnostics.Debug.WriteLine($"Attempting to open:{Environment.NewLine}{item.FilePath}");
			try
			{
				if (item.IsFolder) await Task.Run(() => LoadFolder(item));
				else await LoadArchive(item);
				item.CantOpen = false;
			}
			catch (Exception ex)
			{
				item.CantOpen = true;
				System.Diagnostics.Debug.WriteLine(ex);
				CloseContainer();
				ReplyText = $"Failed - {ex.Message}";
				return;
			}
			if (ContainerModel == null) return;
			if (ContainerModel.TotalFiles == 0)
			{
				ReplyText = "No files found in container";
				MangaInfo = item;
				ContainerModel = null;
				return;
			}
			MangaInfo = item;
			OnPropertyChanged(nameof(TotalFiles));
			GoToIndex(0);
			ReplyText = ContainerModel.TotalFiles + " images.";
			TitleText = $"{Path.GetFileName(item.FilePath)} - {ProgramName}";
			_lastOpened.Add(item);
			item.LastOpened = DateTime.Now;
			LocalDatabase.SaveChanges();
			IndexLabelText = $"/{ContainerModel.TotalFiles}";
		}

		#endregion

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
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
			var type = searchParts.Length == 2 ? searchParts[0].ToLowerInvariant() : "";
			var searchString = (searchParts.Length == 2 ? searchParts[1] : SearchText).Trim().ToLower();
			MangaInfo[] results;
			switch (type)
			{
				case "alias":
					var alias = LocalDatabase.Aliases.FirstOrDefault(x => x.Name.ToLower().Equals(searchString) || x.AliasTags.Any(t => t.Tag.Equals(searchString)));
					if (alias == null) goto case "tag";
					else results = LocalDatabase.AutoTags.Where(x => alias.Tags.Contains(x.Tag.ToLower())).Select(y => y.Item).ToArray();
					break;
				case "tag":
					var tags = searchString.Split('|').Select(t => t.ToLowerInvariant().Trim()).ToArray();
					results = LocalDatabase.AutoTags.Where(x => tags.Contains(x.Tag.ToLower())).Select(y => y.Item).ToArray();
					break;
				case "":
					results = LocalDatabase.Items.Where(x => x.Name.ToLower().Contains(searchString)).ToArray();
					break;
				default:
					throw new ArgumentException("Argument is invalid.");
			}
			if (NoBlacklisted) results = results.Where(x => x.UserTags.All(y => y.Tag.ToLower() != "blacklisted")).ToArray();
			SearchResults.AddRange(results.Distinct().OrderByDescending(x => x.DateAdded));
			DisplayedPanel = DisplayPanel.Library;
			LibraryItemSelector = LibrarySelector.Search;
		}

		public bool NoBlacklisted { get; set; } = false;

		public bool IsFavorite
		{
			get => MangaInfo?.IsFavorite ?? false;
			set
			{
				if (value == MangaInfo.UserTags.Any(x => x.Tag.ToLower().Equals(FavoriteTagString))) return;
				if (value) LocalDatabase.AddTag(MangaInfo, FavoriteTagString);
				else LocalDatabase.RemoveTag(MangaInfo, FavoriteTagString);
				OnPropertyChanged();
			}
		}

		public bool IsBlacklist
		{
			get => MangaInfo?.IsBlacklisted ?? false;
			set
			{
				if (value == MangaInfo.UserTags.Any(x => x.Tag.ToLower().Equals(BlacklistedTagString))) return;
				if (value) LocalDatabase.AddTag(MangaInfo, BlacklistedTagString);
				else LocalDatabase.RemoveTag(MangaInfo, BlacklistedTagString);
				OnPropertyChanged();
			}
		}

		private DisplayPanel _displayedPanel = DisplayPanel.Library;

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

		public int Extracted => ContainerModel?.Extracted ?? 0;
		private int _sliderPage;

		public int SliderPage
		{
			get => _sliderPage;
			set
			{
				_sliderPage = value;
				OnPropertyChanged();
			}
		}

		public IEnumerable<string> Stats { get; }

		public IEnumerable<DeletedMangaInfo> DeletedItems => LocalDatabase.DeletedItems.Local;

		public enum DisplayPanel { Library, Tags, Pages, Stats }

		internal void Search(string searchString)
		{
			SearchText = searchString;
			Search();
		}

		internal void SearchTags(HashSet<string> tags)
		{
			SearchText = $"Tag: {string.Join("|", tags)}";
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

		public void DeleteCurrentItem()
		{
			RemoveItemFromDb(MangaInfo,true,true);
		}

		public bool RemoveItemFromDb(MangaInfo item, bool addToDeletedItems, bool deleteItem)
		{
			if (!UserIsSure()) return false;
			if (item.Id == MangaInfo?.Id) CloseContainer();
			LocalDatabase.DeleteMangaInfo(item,addToDeletedItems);
			if (deleteItem && item.Exists(LocalDatabase.Libraries.First(li=>li.Id == item.LibraryFolderId)))
			{
				var filePath = item.GetFilePath(LocalDatabase.Libraries.First(li => li.Id == item.LibraryFolderId));
				if (item.IsFolder) FileSystem.DeleteDirectory(filePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
				else FileSystem.DeleteFile(filePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
			}
			LogToFile($"Removed item from DB [AddToDeleted={addToDeletedItems}|DeleteFile={deleteItem}] '{item}'");
			return true;
		}

		public async void GetLibraryAdditions()
		{
			int additions = 0;
			ReplyText = "Getting latest additions to library...";
			await Task.Run(() =>
			{
				LocalDatabase.Items.Load();
				foreach (var library in LocalDatabase.Libraries) additions += GetLibraryAdditions(library);
				LocalDatabase.SaveChanges();
			});
			_lastAdded.Items.SetRange(LocalDatabase.GetLastAdded(MaxListItems));
			ReplyText = $"Finished getting new additions ({additions})";
			DisplayedPanel = DisplayPanel.Library;
			LibraryItemSelector = LibrarySelector.Added;
		}

		private int GetLibraryAdditions(LibraryFolder library)
		{
			if (string.IsNullOrWhiteSpace(library.Path)) return 0;
			int additions = 0;
			var directoryInfo = new DirectoryInfo(library.Path);
			var files = directoryInfo.GetFiles().OrderBy(x => x.LastWriteTime).Select(x => x.FullName).ToArray();
			var folders = directoryInfo.GetDirectories().OrderBy(x => x.LastWriteTime).Select(x => x.FullName).ToArray();
			var presentFiles = LocalDatabase.Items.Local.Where(x => !x.IsFolder).Select(x => x.FilePath).ToArray();
			var presentFolders = LocalDatabase.Items.Local.Where(x => x.IsFolder).Select(x => x.FilePath).ToArray();
			int count = 0;
			int total = files.Length + folders.Length;
			bool skipCrcMatch = false;
			foreach (var file in files)
			{
				count++;
				if (count % 10 == 0) ReplyText = $@"{library.Path}: {count}/{total}...";
				if (!FileIsSupported(file)) continue;
				if (presentFiles.Contains(file)) continue;
				additions++;
				var preSavedItem = MangaInfo.Create(file, library, false);
				if (!skipCrcMatch)
				{
					skipCrcMatch = LocalDatabase.CheckCrcMatches(preSavedItem);
				}
				LocalDatabase.Items.Add(preSavedItem);
			}
			foreach (var folder in folders)
			{
				count++;
				if (count % 10 == 0) ReplyText = $@"{library.Path}: {count}/{total}...";
				if (presentFolders.Contains(folder)) continue;
				additions++;
				LocalDatabase.Items.Add(MangaInfo.Create(folder, library, true));
			}
			return additions;
		}

		public MangaInfo GetOrCreateMangaInfo(string containerPath, bool saveToDatabase)
		{
			return LocalDatabase.GetOrCreateMangaInfo(containerPath, _lastAdded, saveToDatabase);
		}
	}

	public enum LibrarySelector
	{
		Added = 0,
		Opened = 1,
		Browsed = 2,
		NotBrowsed = 3,
		Search = 4,
	}
}

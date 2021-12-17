using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
			PageOrderMode = PageOrder.Natural;
			Directory.CreateDirectory(StoredDataFolder);
			try { Directory.Delete(TempFolder, true); }
			catch (IOException) { /*This can fail if folder is open or files are used by other applications.*/ }
			Directory.CreateDirectory(TempFolder);
		}

		public void Initialise(bool loadDatabase)
		{
			if (!loadDatabase) return;
			LocalDatabase.DeletedItems.Load();
			var favoritedTags = LocalDatabase.FavoriteTags.AsEnumerable().Select(ft => ft.Tag.ToLowerInvariant()).Distinct().ToArray();
			var favoriteAliases = LocalDatabase.Aliases.AsEnumerable().Where(a => favoritedTags.Contains(a.Name.ToLowerInvariant()))
				.SelectMany(a => a.AliasTags.Select(at => at.Tag)).Distinct().ToArray();
			var stats = new ObservableCollection<string>()
			{
				$"Total Items: {GetCountAndSize(i=> true)}",
				$"Favorited Items: {GetCountAndSize(IsFavorited())}",
				$"Favorited Tag Items: {GetCountAndSize2(i => HasAutoTag(favoritedTags,i))}",
				$"Favorited Tag Alias Items: {GetCountAndSize2(i => HasAutoTag(favoriteAliases,i))}",
				$"Blacklisted Items: {GetCountAndSize(IsBlacklisted())}",
				$"Browsed Items: {GetCountAndSize(i => i.TimesBrowsed > 0)}",
				$"Not Browsed Items: {GetCountAndSize(i => i.TimesBrowsed == 0)}",
				$"Not Browsed Items (<80 Pages): {GetCountAndSize(i =>i.TimesBrowsed == 0 && i.FileCount < 80)}",
				$"Not Browsed Items (80+ Pages): {GetCountAndSize(i => i.TimesBrowsed == 0 && i.FileCount >= 80)}",
				$"Deleted Items: {DeletedItems.Count()}",
				"Current Session:",
				"Browsed: None",
				"Deleted: None"
		};
			Stats = stats;
			_lastOpened = new RecentItemList<IMangaItem>(MaxListItems, () => LocalDatabase.GetLastOpened(MaxListItems));
			_lastAdded = new RecentItemList<IMangaItem>(MaxListItems, () => LocalDatabase.GetLastAdded(MaxListItems));
			_mostBrowsed = new RecentItemList<IMangaItem>(MaxListItems, () => LocalDatabase.GetMostBrowsed(MaxListItems));
			_notBrowsed = new RecentItemList<IMangaItem>(MaxListItems, () => LocalDatabase.GetNotBrowsed(MaxListItems));
			_shortNotBrowsed = new RecentItemList<IMangaItem>(MaxListItems, () => LocalDatabase.GetRandomContainers((int)(MaxListItems / 2d)));
			OnPropertyChanged(null);
		}

		private int _currentBrowsedCount;
		private long _currentBrowsedSize;
		private int _currentDeletedCount;
		private long _currentDeletedSize;

		public void UpdateStats(bool isBrowsed, long? length)
		{
			if (Stats == null) return;
			if (length == null) return;
			if (isBrowsed)
			{
				_currentBrowsedCount++;
				_currentBrowsedSize += length.Value;
				var browsed = $"{_currentBrowsedCount} items, {GetSize(_currentBrowsedSize)}";
				Stats[Stats.Count - 2] = $"Browsed: {browsed}";
			}
			else
			{
				_currentDeletedCount++;
				_currentDeletedSize += length.Value;
				var deleted = $"{_currentDeletedCount} items, {GetSize(_currentDeletedSize)}";
				Stats[Stats.Count - 1] = $"Deleted: {deleted}";
			}
			OnPropertyChanged(nameof(Stats));
		}


		private static bool HasAutoTag(string[] autoTags, MangaInfo item)
		{
			var result = item.AutoTags.Any(t => autoTags.Contains(t.Tag.ToLower()));
			return result;
		}

		private string GetCountAndSize2(Func<MangaInfo, bool> predicate)
		{
			int count = 0;
			long totalSize = 0;
			foreach (var item in LocalDatabase.Items.Where(predicate))
			{
				count++;
				totalSize += item.Length.GetValueOrDefault();
			}
			double totalSizeMb = totalSize / 1024d / 1024d;
			var sizeString = totalSizeMb > 5000 ? $"{totalSizeMb / 1024:0.00} GB" : totalSizeMb > 100 ? $"{totalSizeMb:0} MB" : $"{totalSizeMb:0.00} MB";
			return $"{count} ({sizeString})";
		}

		private string GetCountAndSize(Expression<Func<MangaInfo, bool>> predicate)
		{
			int count = 0;
			long totalSize = 0;
			foreach (var item in LocalDatabase.Items.Where(predicate))
			{
				count++;
				totalSize += item.Length.GetValueOrDefault();
			}
			return $"{count} ({GetSize(totalSize)})";
		}

		private string GetSize(long totalSize)
		{
			double totalSizeMb = totalSize / 1024d / 1024d;
			var sizeString = totalSizeMb > 5000 ? $"{totalSizeMb / 1024:0.00} GB" : $"{totalSizeMb:0.00} MB";
			return sizeString;
		}

		public const int MaxListItems = 100;
		#region Properties
		private string _rtlToggleText;
		private string _pageModeText;
		private string _pageOrderText;
		private string _rightLabelText;
		private string _leftLabelText;
		private string _titleText;
		private string _replyText;
		private string _indexLabelText;
		private bool _rtlIsChecked;
		private PageMode _pageView;
		private PageOrder _pageOrder;
		private RecentItemList<IMangaItem> _lastOpened = new RecentItemList<IMangaItem>();
		private RecentItemList<IMangaItem> _lastAdded = new RecentItemList<IMangaItem>();
		private RecentItemList<IMangaItem> _mostBrowsed = new RecentItemList<IMangaItem>();
		private RecentItemList<IMangaItem> _notBrowsed = new RecentItemList<IMangaItem>();
		private RecentItemList<IMangaItem> _shortNotBrowsed = new RecentItemList<IMangaItem>();
		private string _searchText;

		public string TitleText { get => _titleText; set { _titleText = value; OnPropertyChanged(); } }

		public string ReplyText
		{
			get => _replyText;
			set
			{
				_replyText = value;
				Debug.WriteLine(value);
				OnPropertyChanged();
			}
		}

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
		public string PageOrderText { get => _pageOrderText; set { _pageOrderText = value; OnPropertyChanged(); } }
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
		private PageOrder PageOrderMode
		{
			get => _pageOrder;
			set
			{
				_pageOrder = value;
				PageOrderText = value switch
				{
					PageOrder.Natural => "Natural Order",
					PageOrder.Modified => "Modified Order",
					PageOrder.Ordinal => "Ordinal Order",
					_ => PageOrderText
				};
			}
		}
		private bool DisplayingOnePage => RightImageSource == DependencyProperty.UnsetValue || LeftImageSource == DependencyProperty.UnsetValue;
		public string SearchText { get => _searchText; set { _searchText = value; OnPropertyChanged(); } }
		public int AutoPlaySpeed { get; set; } = 1000;
		public ObservableCollection<IMangaItem> SearchResults { get; set; } = new ObservableCollection<IMangaItem>();

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
			}
		}
		
		public ObservableCollection<IMangaItem> LibraryItems
		{
			get
			{
				if (LibraryItemSelector == LibrarySelector.Search)
				{
					SearchSettingsVisible = Visibility.Visible;
					return SearchResults;
				}
				SearchSettingsVisible = Visibility.Collapsed;
				var result = LibraryItemSelector switch
				{
					LibrarySelector.Added => _lastAdded,
					LibrarySelector.Opened => _lastOpened,
					LibrarySelector.Browsed => _mostBrowsed,
					LibrarySelector.NotBrowsed => _notBrowsed,
					LibrarySelector.ShortNotBrowsed => _shortNotBrowsed,
					_ => throw new ArgumentOutOfRangeException()
				};
				result.Reset();
				return result.Items;
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
					LibrarySelector.NotBrowsed => "A",
					LibrarySelector.Search => "A",
					LibrarySelector.ShortNotBrowsed => "A",
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

		#endregion

		#region Container-related Properties

		private object _singleImageSource;
		private object _leftImageSource;
		private object _rightImageSource;
		private int _singleRotationAngle;
		private int _leftRotationAngle;
		private int _rightRotationAngle;
		private MangaInfo _mangaInfo;
		private BitmapImage _emptyImage = new BitmapImage();

		public Container ContainerModel { get; private set; }
		public object SingleImageSource
		{
			get => _singleImageSource ?? DependencyProperty.UnsetValue;
			set
			{
				if (_singleImageSource == value) return;
				value = GetImageFromStringOrUri(value);
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
				if (value != null) SingleImageSource = null;
				value = GetImageFromStringOrUri(value);
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
				value = GetImageFromStringOrUri(value);
				_rightImageSource = value;
				OnPropertyChanged();
			}
		}

		public int SingleRotationAngle
		{
			get => _singleRotationAngle;
			set
			{
				if (_singleRotationAngle == value) return;
				_singleRotationAngle = value;
				OnPropertyChanged();
			}
		}
		public int LeftRotationAngle
		{
			get => _leftRotationAngle;
			set
			{
				if (_leftRotationAngle == value) return;
				_leftRotationAngle = value;
				OnPropertyChanged();
			}
		}
		public int RightRotationAngle
		{
			get => _rightRotationAngle;
			set
			{
				if (_rightRotationAngle == value) return;
				_rightRotationAngle = value;
				OnPropertyChanged();
			}
		}

		public MangaInfo MangaInfo
		{
			get => _mangaInfo;
			set
			{
				if (_mangaInfo != null) _mangaInfo.LastOpenPageIndex = ContainerModel?.CurrentIndex;
				_mangaInfo = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsFavorite));
				OnPropertyChanged(nameof(Pages));
				OnPropertyChanged(nameof(TaggedPages));
			}
		}
		public int CurrentIndex
		{
			get => ContainerModel?.CurrentIndex ?? -1;
			set
			{
				ContainerModel.CurrentIndex = value;
				OnPropertyChanged(nameof(CurrentPage));
				OnPropertyChanged(nameof(CurrentPageNotTagged));
				SliderPage = value + 1;
			}
		}
		public int CurrentPage => (ContainerModel?.CurrentIndex ?? -1) + 1;
		public int TotalFiles => ContainerModel?.TotalFiles ?? 1;
		public string[] Pages => ContainerModel?.OrderedFileNames.Select((f, i) => $"[{i:000}] {f}").ToArray() ?? Array.Empty<string>();
		public ObservableCollection<PageTag> TaggedPages => MangaInfo == null ? new ObservableCollection<PageTag>() : new ObservableCollection<PageTag>(MangaInfo.PageTags);
		public bool CurrentPageNotTagged => MangaInfo?.PageTags.All(i => i.Page != CurrentPage) ?? false;

		#endregion

		private object GetImageFromStringOrUri(object value)
		{
			if (value is null) return null;
			BitmapImage bmi = null;
			if (value is string sValue) value = new Uri(sValue);
			if (value is Uri uValue)
			{
				 bmi = new BitmapImage();
				bmi.BeginInit();
				bmi.CacheOption = BitmapCacheOption.OnLoad;
				bmi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile | BitmapCreateOptions.PreservePixelFormat;
				bmi.UriSource = uValue;
				bmi.EndInit();
			}
			else { }
			return bmi ?? value;
		}

		private void PopulateBox(ImageBox imageBox, int index)
		{
			var filename = ContainerModel.GetFile(index, out var displayName);
			PopulateBox(imageBox, index, new Uri(filename), displayName);
		}

		private void PopulateEmpty(ImageBox imageBox)
		{
			PopulateBox(imageBox, -1, null, null);
		}

		private void PopulateBox(ImageBox imageBox, int index, Uri imageUri, string displayName)
		{
			SetImage(imageBox, imageUri);
			SetLabelText(imageBox, string.IsNullOrWhiteSpace(displayName) ? string.Empty : $"({index + 1}) {displayName}");
		}

		private void SetImage(ImageBox boxForImage, Uri imageUri)
		{
			switch (boxForImage)
			{
				case ImageBox.Single:
					SingleImageSource = imageUri;
					SingleRotationAngle = GetRotationAngle(imageUri);
					return;
				case ImageBox.Left:
					LeftImageSource = imageUri;
					LeftRotationAngle = GetRotationAngle(imageUri);
					return;
				case ImageBox.Right:
					RightImageSource = imageUri;
					RightRotationAngle = GetRotationAngle(imageUri);
					return;
				default:
					throw new ArgumentOutOfRangeException(nameof(boxForImage), boxForImage, null);
			}
		}

		private int GetRotationAngle(Uri imageUri)
		{
			if (imageUri == null) return 0;
			const int ExifOrientationTagId = 0x0112;
			var image = System.Drawing.Image.FromFile(imageUri.LocalPath);
			if (!image.PropertyIdList.Contains(ExifOrientationTagId)) return 0;
			var property = image.GetPropertyItem(ExifOrientationTagId);
			if (property == null) return 0;
			var orientation = property.Value[0];

			if (orientation <= 1 || orientation > 8) return 0;
			switch (orientation)
			{
				case 2:
					//image.RotateFlip(RotateFlipType.RotateNoneFlipX);
					throw new NotImplementedException();
				case 3:
					//image.RotateFlip(RotateFlipType.Rotate180FlipNone);
					return 180;
				case 4:
					//image.RotateFlip(RotateFlipType.Rotate180FlipX);
					throw new NotImplementedException();
				case 5:
					//image.RotateFlip(RotateFlipType.Rotate90FlipX);
					throw new NotImplementedException();
				case 6:
					//image.RotateFlip(RotateFlipType.Rotate90FlipNone);
					return 90;
				case 7:
					//image.RotateFlip(RotateFlipType.Rotate270FlipX);
					throw new NotImplementedException();
				case 8:
					//image.RotateFlip(RotateFlipType.Rotate270FlipNone);
					return 270;
			}
			return 0;
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
				ReplyText = "Page out of range.";
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
			var item = LocalDatabase.Items.AsEnumerable()
				.Where(x => GamanDatabase.IsFavoriteOrShortNotBrowsed(x, favorites))
				.OrderBy(_ => Guid.NewGuid()).FirstOrDefault(x => x.Exists());
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
			else if (PageView == PageMode.Dual) moveNumber = 2;
			else
			{
				var bitmap1 = GetImage(ContainerModel.GetFile(CurrentIndex - 1, out _));
				var ratio1 = bitmap1.Width / bitmap1.Height;
				bitmap1.StreamSource = null;
				bitmap1.UriSource = null;
				bitmap1 = null;
				var bitmap2 = GetImage(ContainerModel.GetFile(CurrentIndex - 2, out _));
				var ratio2 = bitmap2.Width / bitmap2.Height;
				bitmap2.StreamSource = null;
				bitmap2.UriSource = null;
				bitmap2 = null;
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
					if(CurrentIndex < 0) PopulateEmpty(imageBox1);
					else PopulateBox(imageBox1, CurrentIndex);
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
			string filename2;
			string displayName2;
			try
			{
				//first, get first image
				imageBox2 = RtlIsChecked ? ImageBox.Left : ImageBox.Right;
				filename2 = ContainerModel.GetFile(CurrentIndex + 1, out displayName2);
				var img2 = GetImage(filename2);
				ratio2 = img2.Width / img2.Height;
				img2.UriSource = null;
				img2.StreamSource = null;
				img2 = null;
				if (CurrentIndex == -1) PopulateEmpty(RtlIsChecked ? ImageBox.Right : ImageBox.Left);
				else
				{
					var filename = ContainerModel.GetFile(CurrentIndex, out var displayName);
					var img1 = GetImage(filename);
					var ratio1 = img1.Width / img1.Height;
					img1.UriSource = null;
					img1.StreamSource = null;
					img1 = null;
					var imageBox1 = ratio1 > 1 || ratio2 > 1 ? ImageBox.Single : (RtlIsChecked ? ImageBox.Right : ImageBox.Left);
					PopulateBox(imageBox1, CurrentIndex, new Uri(filename), displayName);
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
			else PopulateBox(imageBox2, CurrentIndex + 1, new Uri(filename2), displayName2);
		}

		private void PopulateLastImage()
		{
			var filename = ContainerModel.GetFile(CurrentIndex, out var displayName);
			var image = GetImage(filename);
			var wideImage = image.Width > image.Height;
			image.StreamSource = null;
			image.UriSource = null;
			image = null;
			if (wideImage)
			{
				PopulateBox(ImageBox.Single, CurrentIndex, new Uri(filename), displayName);
			}
			else
			{
				PopulateBox(RtlIsChecked ? ImageBox.Right : ImageBox.Left, CurrentIndex, new Uri(filename), displayName);
				if (RtlIsChecked) LeftImageSource = null;
				else RightImageSource = null;
				SetLabelText(RtlIsChecked ? ImageBox.Left : ImageBox.Right, "(none)");
			}
		}


		private static BitmapImage GetImage(string filename)
		{
			//support for other file types goes here
			return RunWithRetries(
				() => InnerGetBitmapImage(filename),
				ex => ex is ArgumentException or IOException or UnauthorizedAccessException,
				5, 50, 1000);
		}

		private static BitmapImage InnerGetBitmapImage(string filename)
		{
			var bmi = new BitmapImage();
			bmi.BeginInit();
			bmi.CacheOption = BitmapCacheOption.OnLoad;
			bmi.CreateOptions = /*BitmapCreateOptions.PreservePixelFormat |*/ BitmapCreateOptions.IgnoreColorProfile;
			bmi.UriSource = new Uri(filename);
			bmi.EndInit();
			return bmi;
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
			switch (extension)
			{
				case ".cbz":
				case ".zip":
					ContainerModel = new ZipContainer(item, OnPropertyChanged, PageOrderMode);
					_tokenSource?.Cancel();
					_zipArchiveTask?.Wait();
					_tokenSource = new CancellationTokenSource();
#pragma warning disable 4014
					Task.Run(() => ZipExtractAllAsync(ContainerModel), CancellationToken.None);
#pragma warning restore 4014
					break;
				case ".rar":
					ContainerModel = await Task.Run(() =>
					{
						var containerModel = new RarContainer(item, OnPropertyChanged, PageOrderMode);
						return containerModel;
					});
					break;
				default:
					throw new InvalidOperationException($"Extension: '{extension}' is not supported.");
			}
		}
		
		private static async Task<Container> LoadArchiveNonUI(MangaInfo item)
		{
			if (!File.Exists(item.FilePath))
			{
				return null;
			}
			var extension = Path.GetExtension(item.FilePath);
			Container container;
			switch (extension)
			{
				case ".cbz":
				case ".zip":
					container = new ZipContainer(item, null, PageOrder.Natural);
					break;
				case ".rar":
					container = await Task.Run(() =>
					{
						var containerModel = new RarContainer(item, null, PageOrder.Natural, doNotExtract:true);
						return containerModel;
					});
					break;
				default:
					throw new InvalidOperationException($"Extension: '{extension}' is not supported.");
			}
			return container;
		}

		private async Task ZipExtractAllAsync(Container container)
		{
			await ((ZipContainer)container).ExtractAllAsync(_tokenSource.Token);
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
			ContainerModel = new FolderContainer(item, files, PageOrderMode);
		}

		private static FolderContainer LoadFolderNonUI(MangaInfo item)
		{
			if (!Directory.Exists(item.FilePath))
			{
				return null;
			}
			var directoryInfo = new DirectoryInfo(item.FilePath);
			var files = MangaInfo.GetImageFiles(directoryInfo, SearchOption.TopDirectoryOnly);
			var folders = Directory.GetDirectories(item.FilePath);
			if (files.Length + folders.Length == 0)
			{
				return null;
			}
			if (folders.Any())
			{
				var result = MessageBox.Show($"{item.NoTagName}{Environment.NewLine}Include sub-folders?", ProgramName, MessageBoxButton.YesNo);
				if (result == MessageBoxResult.Yes)
				{
					files = MangaInfo.GetImageFiles(directoryInfo, SearchOption.AllDirectories);
				}
			}
			return new FolderContainer(item, files, PageOrder.Natural);
		}

		public async Task LoadContainer(MangaInfo item, bool saveToDatabase = true)
		{
			if (MangaInfo == item) return;
			MangaInfo = null;
			ContainerModel = null;
			LeftImageSource = null;
			RightImageSource = null;
			SingleImageSource = null;
			ReplyText = $"Attempting to open {item.Name}";
			if (item.IsDeleted)
			{
				ReplyText = $"Item has been deleted from database: {item}";
				return;
			}
			if (saveToDatabase && string.IsNullOrWhiteSpace(item.CRC32)) await Task.Run(item.Initialise);
			var itemExists = await Task.Run(() => item.Exists(saveToDatabase ? null : LocalDatabase.DefaultLibrary));
			if (!itemExists)
			{
				CloseContainer();
				var failedLog = $"Failed - File or folder did not exist: {item.FilePath}";
				ReplyText = failedLog;
				return;
			}
			try
			{
				await Task.Run(async () =>
				{
					if (item.IsFolder) LoadFolder(item);
					else await LoadArchive(item);
					item.CantOpen = false;
				});
				if (saveToDatabase) GetThumbInBackground(item);
			}
			catch (Exception ex)
			{
				item.CantOpen = true;
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
			var lastOpenPage = MangaInfo.LastOpenPageIndex.GetValueOrDefault(0);
			if (lastOpenPage >= TotalFiles) lastOpenPage = 0;
			GoToIndex(lastOpenPage);
			ReplyText = string.Empty;
			TitleText = $"{Path.GetFileName(item.FilePath)} - {ProgramName}";
			_lastOpened.Add(item);
			item.LastOpened = DateTime.Now;
			LocalDatabase.SaveChanges();
			IndexLabelText = $"/{ContainerModel.TotalFiles}";
		}

		public static async Task<Container> LoadContainerNonUI(MangaInfo item)
		{
			if (item.IsDeleted) return null;
			var itemExists = await Task.Run(() => item.Exists());
			if (!itemExists)
			{
				return null;
			}
			Container container = null;
			try
			{
				await Task.Run(async () =>
				{
					if (item.IsFolder) container = LoadFolderNonUI(item);
					else container = await LoadArchiveNonUI(item);
				});
			}
			catch (Exception)
			{
				return null;
			}
			if (container == null) return null;
			if (container.TotalFiles == 0)
			{
				return null;
			}
			return container;
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

		private static bool FileIsSupported(string file) => RecognizedContainers.Contains(Path.GetExtension(file));

		public void Search(bool ignoreSingleResult)
		{
			//split through pipe, then split through comma but add with comma also
			Func<string, string[]> getParts = s => s
				.Split('|')
				.SelectMany(t => t
					.Split(',')
					.Concat(new[] { t })
					.Select(st => st.ToLower().Trim()))
				.Distinct()
				.ToArray();
			var searchParts = SearchText.Split(':');
			var type = searchParts.Length == 2 ? searchParts[0].ToLower() : "";
			var searchString = (searchParts.Length == 2 ? searchParts[1] : SearchText).Trim().ToLower();
			List<MangaInfo> activeResults = new List<MangaInfo>();
			List<DeletedMangaInfo> deletedResults = new List<DeletedMangaInfo>();
			// ReSharper disable AssignNullToNotNullAttribute
			switch (type)
			{
				case "alias":
					var parts = getParts(searchString);
					var aliases = parts.Select(searchPart => LocalDatabase.Aliases.FirstOrDefault(x => x.Name.ToLower().Equals(searchPart) || x.AliasTags.Any(t => t.Tag.Equals(searchPart)))).Where(a => a != null).ToList();
					activeResults.AddRange(aliases.SelectMany(alias => LocalDatabase.AutoTags.AsEnumerable().Where(x => alias.Tags.Contains(x.Tag.ToLower()) && x.Item != null).Select(y => y.Item)));
					deletedResults.AddRange(aliases.SelectMany(alias => LocalDatabase.DeletedItems.AsEnumerable().Where(d => alias.Tags.Any(t => d.Name.ToLower().Contains(t)))));
					//todo temporary because tags are deleted along with items
					deletedResults.AddRange(LocalDatabase.DeletedItems.AsEnumerable().Where(d => parts.Any(p=> d.Name.ToLower().Contains(p))));
					goto case "tag";
				case "tag":
					var tags = getParts(searchString);
					var idsOfItemsWithTags = LocalDatabase.AutoTags.AsEnumerable().Where(x => tags.Contains(x.Tag.ToLower())).Select(y => y.ItemId).Distinct().ToArray();
					activeResults.AddRange(LocalDatabase.Items.Where(d => idsOfItemsWithTags.Contains(d.Id)));
					deletedResults.AddRange(LocalDatabase.DeletedItems.Where(d => idsOfItemsWithTags.Contains(d.Id)));
					break;
				case "":
					activeResults.AddRange(LocalDatabase.Items.AsEnumerable().Where(x => x.Name.ToLower().Contains(searchString)));
					deletedResults.AddRange(LocalDatabase.DeletedItems.AsEnumerable().Where(d => d.Name.ToLower().Contains(searchString)));
					break;
				default:
					throw new ArgumentException("Argument is invalid.");
			}
			// ReSharper restore AssignNullToNotNullAttribute
			var results = activeResults.AsEnumerable<IMangaItem>().Concat(deletedResults);
			if (NoBlacklisted) results = results.Where(x => x is MangaInfo { IsBlacklisted: false });
			var resultArray = results.Distinct().OrderByDescending(x => x.DateAdded);
			var finalResults = resultArray.Take(MaxListItems).ToArray();
			if (ignoreSingleResult && finalResults.Length == 1)
			{
				ReplyText = $"No other results.";
				return;
			}
			SearchResults.SetRange(finalResults);
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

		private DisplayPanel _displayedPanel = DisplayPanel.Library;

		public object DisplayedPanel
		{
			get => (int)_displayedPanel;
			set
			{
				_displayedPanel = value switch
				{
					int iVal => (DisplayPanel)iVal,
					DisplayPanel eVal => eVal,
					_ => throw new ArgumentException("Must pass object of type DisplayPanel or int")
				};
				OnPropertyChanged();
			}
		}

		public int Extracted => ContainerModel?.Extracted ?? 0;
		private int _sliderPage;
		private Visibility _searchSettingsVisible = Visibility.Collapsed;

		public int SliderPage
		{
			get => _sliderPage;
			set
			{
				_sliderPage = value;
				OnPropertyChanged();
			}
		}

		public ObservableCollection<string> Stats { get; set; }

		public IEnumerable<DeletedMangaInfo> DeletedItems => LocalDatabase.DeletedItems.Local;

		public Visibility SearchSettingsVisible
		{
			get => _searchSettingsVisible;
			set
			{
				_searchSettingsVisible = value;
				OnPropertyChanged();
			}
		}

		public enum DisplayPanel { Library, Tags, Pages, Stats }

		internal void Search(string searchString, bool ignoreSingleResult)
		{
			SearchText = searchString;
			Search(ignoreSingleResult);
		}

		internal void SearchTags(HashSet<string> tags, bool ignoreSingleResult)
		{
			SearchText = $"Alias: {string.Join("|", tags)}";
			Search(ignoreSingleResult);
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

		public void ChangePageOrder()
		{
			switch (PageOrderMode)
			{
				case PageOrder.Natural:
					PageOrderMode = PageOrder.Modified;
					break;
				case PageOrder.Modified:
					PageOrderMode = PageOrder.Ordinal;
					break;
				case PageOrder.Ordinal:
					PageOrderMode = PageOrder.Natural;
					break;
			}
		}

		private enum ImageBox { Single, Left, Right }
		private enum PageMode { Single, Dual, Auto }
		public enum PageOrder { Natural, Modified, Ordinal }

		public bool RemoveItemFromDb(MangaInfo item, bool addToDeletedItems, bool deleteItem)
		{
			if (!UserIsSure(@$"Are you sure you wish to remove item '{item}'?")) return false;
			if (item.Id == MangaInfo?.Id) CloseContainer();
			LocalDatabase.DeleteMangaInfo(item, addToDeletedItems);
			item.IsDeleted = true;
			if (deleteItem && item.Exists(LocalDatabase.Libraries.First(li => li.Id == item.LibraryFolderId)))
			{
				var filePath = item.GetFilePath(LocalDatabase.Libraries.First(li => li.Id == item.LibraryFolderId));
				if (item.IsFolder) FileSystem.DeleteDirectory(filePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
				else FileSystem.DeleteFile(filePath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
			}
			LogToFile($"Removed item from DB [AddToDeleted={addToDeletedItems}|DeleteFile={deleteItem}] '{item}'");
			UpdateStats(false, item.Length);
			return true;
		}

		public async void GetLibraryAdditions()
		{
			int additions = 0;
			ReplyText = "Getting latest additions to library...";
			await Task.Run(() =>
			{
				LocalDatabase.Items.Load();
				additions = LocalDatabase.Libraries.Sum(GetLibraryAdditions);
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
				GetThumbInBackground(preSavedItem);
			}
			foreach (var folder in folders)
			{
				count++;
				if (count % 10 == 0) ReplyText = $@"{library.Path}: {count}/{total}...";
				if (presentFolders.Contains(folder)) continue;
				additions++;
				var preSavedItem = MangaInfo.Create(folder, library, true);
				LocalDatabase.Items.Add(preSavedItem);
				GetThumbInBackground(preSavedItem);
			}
			return additions;
		}

		public static void GetThumbInBackground(IMangaItem item)
		{
			if (item.ThumbnailSet) return;
			Task.Run(item.EnsureThumbExists);
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
		ShortNotBrowsed = 5,
	}
}

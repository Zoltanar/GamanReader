using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using GamanReader.Model;
using SevenZip;
using WpfAnimatedGif;
using Image = System.Windows.Controls.Image;

namespace GamanReader.ViewModel
{
	public class MainViewModel : INotifyPropertyChanged
	{

		public MainViewModel()
		{
			_mainWindow = Application.Current.MainWindow as View.MainWindow;
			RtlIsChecked = false;
			DualPageIsChecked = false;
		}

		internal void GoBack(bool moveOne)
		{
			if (_containerModel == null) return;
			if (!_containerModel.HasPrevious) return;
			_containerModel.GoBack(moveOne ? 1 : PageSize);
		}
		public void GoForward(bool moveOne)
		{
			if (_containerModel == null) return;
			if (_containerModel.FilesAhead < PageSize) return;
			_containerModel.GoForward(moveOne ? 1 : PageSize);
		}

		#region Properties
		private readonly View.MainWindow _mainWindow;
		private ContainerViewModel _containerModel;
		private string _rtlToggleText;
		private string _pageSizeToggleText;
		private string _rightLabelText;
		private string _leftLabelText;
		private string _tagText;
		private string _replyText;
		private string _indexLabelText;
		private string _goToIndexText;
		private bool? _rtlIsChecked;
		private bool? _dualPageIsChecked;
		private RecentItemList<string> _recentFiles = new RecentItemList<string>(Settings.RecentListSize, Settings.RecentFolders);

		public string ReplyText { get => _replyText; set { _replyText = value; OnPropertyChanged("ReplyText"); } }
		public string TagText { get => _tagText; set { _tagText = value; OnPropertyChanged("TagText"); } }
		public string LeftLabelText { get => _leftLabelText; set { _leftLabelText = value; OnPropertyChanged("LeftLabelText"); } }
		public string RightLabelText { get => _rightLabelText; set { _rightLabelText = value; OnPropertyChanged("RightLabelText"); } }
		public string RtlToggleText { get => _rtlToggleText; set { _rtlToggleText = value; OnPropertyChanged("RtlToggleText"); } }
		public string PageSizeToggleText { get => _pageSizeToggleText; set { _pageSizeToggleText = value; OnPropertyChanged("PageSizeToggleText"); } }
		public string IndexLabelText { get => _indexLabelText; set { _indexLabelText = value; OnPropertyChanged("IndexLabelText"); } }
		public string GoToIndexText { get => _goToIndexText; set { _goToIndexText = value; OnPropertyChanged("GoToIndexText"); } }
		public bool? RtlIsChecked
		{
			get => _rtlIsChecked ?? false;
			set
			{
				if (!value.HasValue) value = false;
				_rtlIsChecked = value;
				RtlToggleText = value.Value ? "◀ Right-to-Left ◀" : "▶ Left-to-Right ▶";
				OnPropertyChanged("RtlIsChecked");
				if (_containerModel == null) return;
				PopulateBox(ImageBox.Single, -1);
				PopulateBox(ImageBox.Left, -1);
				PopulateBox(ImageBox.Right, -1);
				_containerModel.GoForward(0);
			}
		}
		public bool? DualPageIsChecked
		{
			get => _dualPageIsChecked ?? false;
			set
			{
				if (!value.HasValue) value = false;
				_dualPageIsChecked = value;
				PageSize = _dualPageIsChecked.Value ? 2 : 1;
				PageSizeToggleText = value.Value ? "Dual Page View" : "Single Page View";
				OnPropertyChanged("DualPageIsChecked");
				if (_containerModel == null) return;
				PopulateBox(ImageBox.Single, -1);
				PopulateBox(ImageBox.Left, -1);
				PopulateBox(ImageBox.Right, -1);
				_containerModel.GoForward(0);
			}
		}
		public ObservableCollection<string> RecentItems { get => _recentFiles.Items;  }

		// Create the OnPropertyChanged method to raise the event
		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		public int PageSize { get; private set; }

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		internal void PopulateBox(ImageBox imagebox,int index)
		{
			var image = GetImage(imagebox);
			var filename = _containerModel.GetFile(index);
			if (filename == null)
			{
				image.Source = null;
				SetLabelText(imagebox, "");
				return;
			}
			SetLabelText(imagebox, $"({index + 1}){Path.GetFileName(filename)}");
			if (Path.GetExtension(filename).Equals(".gif"))
				ImageBehavior.SetAnimatedSource(image, new BitmapImage(new Uri(filename)));
			else
			{
				ImageBehavior.SetAnimatedSource(image, null);
				image.Source = new BitmapImage(new Uri(filename));
			}
			Image GetImage(ImageBox boxForGetImage)
			{
				switch (boxForGetImage)
				{
					case ImageBox.Single: return _mainWindow.SingleImageBox;
					case ImageBox.Left: return _mainWindow.LeftImageBox;
					case ImageBox.Right: return _mainWindow.RightImageBox;
					default: throw new Exception($"Unexpected ImageBox value! ({boxForGetImage})");
				}
			}
			void SetLabelText(ImageBox boxForLabelText, string text)
			{
				switch (boxForLabelText)
				{
					case ImageBox.Single:
						if (RtlIsChecked.Value) RightLabelText = text;
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
		}

		internal void GoToIndex(int pageNumber)
		{
			if (_containerModel == null) return;
			GoToPage(pageNumber);
		}

		private void GoToPage(int pageNumber)
		{
			_containerModel.CurrentIndex = pageNumber - 1;
			_containerModel.PopulatePreviousBox(_containerModel.CurrentIndex);
			if (PageSize == 2) _containerModel.PopulateNextBox(_containerModel.HasNext ? _containerModel.CurrentIndex+1 : -1);
			GoToIndexText = (_containerModel.CurrentIndex + 1).ToString();
			IndexLabelText = $"/{_containerModel.TotalFiles}";
		}

		internal void ChangePageSize(int newPageSize)
		{
			PageSize = newPageSize;
			if (newPageSize == 1)
			{
				if (RtlIsChecked.Value) LeftLabelText = null;
				else RightLabelText = null;
			}
			if (_containerModel != null) _containerModel.GoForward(0);
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
			_containerModel = new ArchiveViewModel(archivePath, zipFile.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af => af.FileName), this);
			_containerModel.Initialize();
			ReplyText = _containerModel.TotalFiles + " files in folder.";
			_mainWindow.ChangeTitle(Path.GetFileName(archivePath));
			_recentFiles.Add(_containerModel.ContainerPath);
			GoToIndexText = (_containerModel.CurrentIndex + 1).ToString();
			IndexLabelText = $"/{_containerModel.TotalFiles}";
			Settings.Save(_recentFiles.Items);
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
			if (!files.Any())
			{
				if (!folders.Any())
				{
					ReplyText = "No files in folder.";
					return;
				}
				files = folders.SelectMany(f => Directory.GetFiles(f, "*", SearchOption.AllDirectories)).ToList();
			}
			else
			{
				if (folders.Any())
				{
					var result = MessageBox.Show("Include sub-folders?", "Include Folders", MessageBoxButton.YesNo);
					if (result == MessageBoxResult.Yes)
					{
						files.AddRange(Directory.GetFiles(folderName, "*", SearchOption.AllDirectories));
					}
				}
			}
			_containerModel = new FolderViewModel(folderName, files, this);
			_containerModel.Initialize();
			ReplyText = _containerModel.TotalFiles + " images.";
			_mainWindow.ChangeTitle(Path.GetFileName(folderName));
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
	}
}

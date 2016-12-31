using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SevenZip;
using WpfAnimatedGif;
using static GamanReader.StaticHelpers;

namespace GamanReader
{
    /// <summary>
    /// WPF application for reading manga with a dual-page view.
    /// </summary>
    public partial class MainWindow
    {


        private ViewModel _viewModel;
        private int _pageSize = 2; //make switchable to 1 (or more than 2)
        private bool _rtlDirection; //false for left to right, true for right to left
        private RecentItemList<string> _recentFiles;


        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(StoredDataFolder);
            Directory.CreateDirectory(TempFolder);
            foreach (var file in Directory.GetFiles(TempFolder))
            {
                File.Delete(file);
            }
            LoadConfig();
            //
            var firstPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (firstPath == null)
            {
                //TODO return library not found error
                return;
            }
            string path = Path.Combine(firstPath, "7z.dll");
            if (!File.Exists(path))
            {
                //TODO return library not found error
                return;
            }
            //TODO try default 7zip install folder 
            SevenZipBase.SetLibraryPath(path);
            //
            RecentCb.ItemsSource = _recentFiles.Items;
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1) LoadFolder(args[1]);
        }

        private void LoadFolderByDialog(object sender, RoutedEventArgs e)
        {
            var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
            var result = folderPicker.ShowDialog();
            if (result != CommonFileDialogResult.Ok) return;
            LoadFolder(folderPicker.FileName);
        }

        private void LoadArchiveByDialog(object sender, RoutedEventArgs e)
        {
            var folderPicker = new OpenFileDialog { Filter = "Archives|*.zip;*.rar" };
            bool? resultOk = folderPicker.ShowDialog();
            if (resultOk != true) return;
            LoadArchive(folderPicker.FileName);
        }



        private void LoadArchive(string archivePath)
        {
            if (!File.Exists(archivePath))
            {
                ReplyLabel.Content = "Archive doesn't exist.";
                return;
            }
            SevenZipExtractor zipFile = new SevenZipExtractor(archivePath);
            _viewModel = new ArchiveViewModel(archivePath, zipFile.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af => af.FileName));
            ReplyLabel.Content = _viewModel.TotalFiles + " files in folder.";
            Title = $"{Path.GetFileName(archivePath)} - {ProgramName}";
            PopulatePreviousBox(_viewModel.GetFile());
            PopulateNextBox(_viewModel.TotalFiles > 1 ? _viewModel.GetFileForward() : null);
            _recentFiles.Add(_viewModel.ContainerPath);
            IndexLabel.Content = $"{_viewModel.CurrentIndex + 1}/{_viewModel.TotalFiles}";
            SaveConfig();
        }

        private void LoadFolder(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                ReplyLabel.Content = "Folder doesn't exist.";
                return;
            }
            var files = Directory.GetFiles(folderName).ToList();
            var folders = Directory.GetDirectories(folderName);
            if (!files.Any())
            {
                if (!folders.Any())
                {
                    ReplyLabel.Content = "No files in folder.";
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
            _viewModel = new FolderViewModel(folderName, files);
            ReplyLabel.Content = _viewModel.TotalFiles + " images.";
            Title = $"{Path.GetFileName(folderName)} - {ProgramName}";
            PopulatePreviousBox(_viewModel.GetFile());
            PopulateNextBox(_viewModel.TotalFiles > 1 ? _viewModel.GetFileForward() : null);
            _recentFiles.Add(_viewModel.ContainerPath);
            IndexLabel.Content = $"{_viewModel.CurrentIndex + 1}/{_viewModel.TotalFiles}";
            SaveConfig();
        }

        private void SaveConfig()
        {
            XmlHelper.ToXmlFile(new ConfigXml(_recentFiles.Items.ToList(), _recentFiles.Size), ConfigPath);
        }

        private void LoadConfig()
        {
            var config = File.Exists(ConfigPath) ? XmlHelper.FromXmlFile<ConfigXml>(ConfigPath) : new ConfigXml();
            _recentFiles = new RecentItemList<string>(size: config.RecentListSize, items: config.RecentFolders);
        }

        private void LoadContainer(string containerPath)
        {
            if (Directory.Exists(containerPath)) LoadFolder(containerPath);
            else if (File.Exists(containerPath)) LoadArchive(containerPath);
            else ReplyLabel.Content = "Container doesn't exist.";
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

        private void PopulatePreviousBox(string filename)
        {
            Image imagebox;
            if (_pageSize == 1) imagebox = SingleImageBox;
            else imagebox = _rtlDirection ? RightImageBox : LeftImageBox;
            Label label = _rtlDirection ? RightSideLabel : LeftSideLabel;
            PopulateBox(imagebox, filename);
            label.Content = $"({_viewModel.CurrentIndex + 1}){Path.GetFileName(filename)}";
        }

        private void PopulateNextBox(string filename)
        {
            if (_pageSize == 1) return;
            Image imagebox = _rtlDirection ? LeftImageBox : RightImageBox;
            Label label = _rtlDirection ? LeftSideLabel : RightSideLabel;
            if (filename == null)
            {
                imagebox.Source = null;
                label.Content = null;
                return;
            }
            PopulateBox(imagebox, filename);
            label.Content = $"({_viewModel.CurrentIndex + 2}){Path.GetFileName(filename)}";
        }

        private static void PopulateBox(Image imagebox, string filename)
        {
            if (filename == null) return;
            if (Path.GetExtension(filename).Equals(".gif"))
                ImageBehavior.SetAnimatedSource(imagebox, new BitmapImage(new Uri(filename)));
            else
            {
                ImageBehavior.SetAnimatedSource(imagebox, null);
                imagebox.Source = new BitmapImage(new Uri(filename));
            }
        }

        private void GoToPage(int pageNumber)
        {
            _viewModel.CurrentIndex = pageNumber - 1;
            PopulatePreviousBox(_viewModel.GetFile());
            if (_pageSize == 2) PopulateNextBox(_viewModel.HasNext ? _viewModel.GetFileForward() : null);
            IndexLabel.Content = $"{_viewModel.CurrentIndex + 1}/{_viewModel.TotalFiles}";
        }

        private void GoBack(int moveNumber)
        {
            if (_viewModel.CurrentIndex == 0) return;
            if (moveNumber == 1)
            {
                _viewModel.CurrentIndex--;
            }
            else if (_viewModel.HasTwoPrevious)
            {
                _viewModel.CurrentIndex -= _pageSize;
            }
            else
            {
                _viewModel.CurrentIndex--;
            }
            PopulatePreviousBox(_viewModel.GetFile());
            if (_pageSize == 2) PopulateNextBox(_viewModel.HasNext ? _viewModel.GetFileForward() : null);
            IndexLabel.Content = $"{_viewModel.CurrentIndex + 1}/{_viewModel.TotalFiles}";

        }

        private void GoForward(int moveNumber)
        {
            if (_viewModel.CurrentIndex == _viewModel.TotalFiles - 1) return;
            if (moveNumber == 0)
            {
                PopulatePreviousBox(_viewModel.GetFile());
                if (_pageSize == 2) PopulateNextBox(_viewModel.HasNext ? _viewModel.GetFileForward() : null);
            }
            else if (moveNumber == 1)
            {
                _viewModel.CurrentIndex++;
            }
            else if (_viewModel.FilesAhead >= 3)
            {
                _viewModel.CurrentIndex += _pageSize;
            }
            else
            {
                _viewModel.CurrentIndex++;
            }
            PopulatePreviousBox(_viewModel.GetFile());
            if (_pageSize == 2) PopulateNextBox(_viewModel.HasNext ? _viewModel.GetFileForward() : null);
            IndexLabel.Content = $"{_viewModel.CurrentIndex + 1}/{_viewModel.TotalFiles}";

        }

        private void MakeLtr(object sender, RoutedEventArgs e)
        {
            DirectionButton.Content = "▶ Left-to-Right ▶";
            _rtlDirection = false;
            GoForward(0);
        }

        private void MakeRtl(object sender, RoutedEventArgs e)
        {
            DirectionButton.Content = "◀ Right-to-Left ◀";
            _rtlDirection = true;
            GoForward(0);
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
        

        private void MakeDualPage(object sender, RoutedEventArgs e)
        {
            DualSingleSwitcher.Content = "Dual Page View";
            _pageSize = 2;
            SingleImageBox.Source = null;
            GoForward(0);
        }

        private void MakeSinglePage(object sender, RoutedEventArgs e)
        {
            DualSingleSwitcher.Content = "Single Page View";
            _pageSize = 1;
            ImageBehavior.SetAnimatedSource(LeftImageBox, null);
            ImageBehavior.SetAnimatedSource(RightImageBox, null);
            LeftImageBox.Source = null;
            RightImageBox.Source = null;
            RightSideLabel.Content = null;
            GoForward(0);
        }

        private void GoToTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;
            if (e.Key != Key.Enter) return;
            int pageNumber;
            if (!int.TryParse(GoToTextBox.Text, out pageNumber))
            {
                //TODO ERROR
                return;
            }
            e.Handled = true;
            GoToPage(pageNumber);
            GoToTextBox.Text = "";
        }
    }
}

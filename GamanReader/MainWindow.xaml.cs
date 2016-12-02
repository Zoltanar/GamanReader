﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SevenZip;
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
            if(args.Length > 1) LoadFolder(args[1]);
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
            var folderPicker = new OpenFileDialog {Filter = "Archives|*.zip;*.rar"};
            bool? resultOk = folderPicker.ShowDialog();
            if (resultOk != true) return;
            LoadArchive(folderPicker.FileName);
        }
        
        private void LoadArchive(string containerPath)
        {
            var extension = Path.GetExtension(containerPath);
            if (extension != null && extension.Equals(".zip")) LoadZip(containerPath);
            if (extension != null && extension.Equals(".rar")) LoadRar(containerPath);
        }

        // ReSharper disable once UnusedParameter.Local
        private void LoadRar(string archivePath)
        {
            //TODO
            throw new NotImplementedException();
        }

        private void LoadZip(string archivePath)
        {
            if (!File.Exists(archivePath))
            {
                ReplyLabel.Content = "Archive doesn't exist.";
                return;
            }
            SevenZipExtractor zipFile = new SevenZipExtractor(archivePath);
            _viewModel = new ZipViewModel(archivePath, zipFile.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af=>af.FileName));
            ReplyLabel.Content = _viewModel.TotalFiles + " files in folder.";
            Title = $"{Path.GetFileName(archivePath)} - {ProgramName}";
            PopulatePreviousBox(_viewModel.GetFile());
            if (_viewModel.TotalFiles > 1) PopulateNextBox(_viewModel.GetFileForward());
            _recentFiles.Add(_viewModel.ContainerPath);
            IndexLabel.Content = $"{_viewModel.CurrentIndex + 1}/{_viewModel.TotalFiles}";
            SaveConfig();

            //

        }

        private void LoadFolder(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                ReplyLabel.Content = "Folder doesn't exist.";
                return;
            }
            var files = Directory.GetFiles(folderName);
            if (!files.Any())
            {
                var folders = Directory.GetDirectories(folderName);
                if (!folders.Any())
                {
                    ReplyLabel.Content = "No files in folder.";
                    return;
                }
                files = folders.SelectMany(Directory.GetFiles).ToArray();
            }
            _viewModel = new FolderViewModel(folderName, files);
            ReplyLabel.Content = _viewModel.TotalFiles + " files in folder.";
            Title = $"{Path.GetFileName(folderName)} - {ProgramName}";
            PopulatePreviousBox(_viewModel.GetFile());
            if (_viewModel.TotalFiles > 1) PopulateNextBox(_viewModel.GetFileForward());
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

        private void RecentCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0) return;
            if (e.AddedItems[0] != null && !e.AddedItems[0].ToString().Equals(""))
            {
                var containerPath = e.AddedItems[0].ToString();
                if(Directory.Exists(containerPath)) LoadFolder(containerPath);
                else if (File.Exists(containerPath)) LoadArchive(containerPath);
                else ReplyLabel.Content = "Container doesn't exist.";
                //TODO clear from list if it doesnt exist
            }
        }


        private void PopulatePreviousBox(string filename)
        {
            Image imagebox = _rtlDirection ? RightImageBox : LeftImageBox;
            Label label = _rtlDirection ? RightSideLabel : LeftSideLabel;
            imagebox.Source = new BitmapImage(new Uri(filename));
            label.Content = $"({_viewModel.CurrentIndex}){Path.GetFileName(filename)}";
        }
        
        private void PopulateNextBox(string filename)
        {
            Image imagebox = _rtlDirection ? LeftImageBox : RightImageBox;
            Label label = _rtlDirection ? LeftSideLabel : RightSideLabel;
            imagebox.Source = new BitmapImage(new Uri(filename));
            label.Content = $"({_viewModel.CurrentIndex + 1}){Path.GetFileName(filename)}";
        }

        private void HandleKeys(object sender, KeyEventArgs e)
        {
            if (_viewModel == null) return;
            switch (e.Key)
            {
                case Key.Left:
                    if (_rtlDirection)
                    {
                        if (_viewModel.FilesAhead >= 2)
                        {
                            GoForward(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    else
                    {
                        if (_viewModel.HasPrevious)
                        {
                            GoBack(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    return;
                case Key.Right:
                    if (_rtlDirection)
                    {
                        if (_viewModel.HasPrevious)
                        {
                            GoBack(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    else
                    {
                        if (_viewModel.FilesAhead >= 2)
                        {
                            GoForward(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    return;
            }
        }

        private void GoBack(int moveNumber)
        {
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
            if (_viewModel.HasNext) PopulateNextBox(_viewModel.GetFileForward());
            IndexLabel.Content = $"{_viewModel.CurrentIndex + 1}/{_viewModel.TotalFiles}";

        }

        private void GoForward(int moveNumber)
        {
            if (moveNumber == 0)
            {
                PopulatePreviousBox(_viewModel.GetFile());
                if (_viewModel.HasNext) PopulateNextBox(_viewModel.GetFileForward());
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
            if (_viewModel.HasNext) PopulateNextBox(_viewModel.GetFileForward());
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

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_viewModel == null) return;
            if (e.Delta < 0)
            {
                if (_viewModel.FilesAhead >= 2)
                {
                    GoForward(CtrlIsDown() ? 1 : _pageSize);
                }
            }
            else if (e.Delta > 0)
            {
                if (_viewModel.HasPrevious)
                {
                    GoBack(CtrlIsDown() ? 1 : _pageSize);
                }
            }
        }

    }
}

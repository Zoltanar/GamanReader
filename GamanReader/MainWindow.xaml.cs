using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace GamanReader
{
    /// <summary>
    /// WPF application for reading manga with a dual-page view.
    /// </summary>
    public partial class MainWindow
    {
        private ReaderViewModel _rvm;
        private int _pageSize = 2;
        private bool _rtlDirection; //false for left to right, true for right to left
        public MainWindow()
        {
            InitializeComponent();
        }

        private void LoadFolder(object sender, RoutedEventArgs e)
        {
            var folderPicker = new CommonOpenFileDialog { IsFolderPicker = true, AllowNonFileSystemItems = true };
            var result = folderPicker.ShowDialog();
            if (result != CommonFileDialogResult.Ok) return;
            var files = Directory.GetFiles(folderPicker.FileName);
            if (!files.Any())
            {
                ReplyLabel.Content = "No files in folder.";
                return;
            }
            _rvm = new ReaderViewModel(folderPicker.FileName, files);
            ReplyLabel.Content = _rvm.TotalFiles + " files in folder.";
            PopulatePreviousBox(_rvm.GetFile);
            if (_rvm.TotalFiles > 1) PopulateNextBox(_rvm.GetFileForward);
        }

        private void PopulatePreviousBox(string filename)
        {
            Image imagebox = _rtlDirection ? RightImageBox : LeftImageBox;
            Label label = _rtlDirection ? RightSideLabel : LeftSideLabel;
            imagebox.Source = new BitmapImage(new Uri(filename));
            label.Content = $"({_rvm.CurrentIndex}){Path.GetFileName(filename)}";
        }


        private void PopulateNextBox(string filename)
        {
            Image imagebox = _rtlDirection ? LeftImageBox : RightImageBox;
            Label label = _rtlDirection ? LeftSideLabel : RightSideLabel;
            imagebox.Source = new BitmapImage(new Uri(filename));
            label.Content = $"({_rvm.CurrentIndex + 1}){Path.GetFileName(filename)}";
        }

        private void HandleKeys(object sender, KeyEventArgs e)
        {
            if (_rvm == null) return;
            switch (e.Key)
            {
                case Key.Left:
                    if (_rtlDirection)
                    {
                        if (_rvm.FilesAhead >= 2)
                        {
                            GoForward(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    else
                    {
                        if (_rvm.HasPrevious)
                        {
                            GoBack(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    return;
                case Key.Right:
                    if (_rtlDirection)
                    {
                        if (_rvm.HasPrevious)
                        {
                            GoBack(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    else
                    {
                        if (_rvm.FilesAhead >= 2)
                        {
                            GoForward(CtrlIsDown() ? 1 : _pageSize);
                        }
                    }
                    return;
            }
        }

        public static bool CtrlIsDown()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private void GoBack(int moveNumber)
        {
            if (moveNumber == 1)
            {
                _rvm.CurrentIndex--;
            }
            else if (_rvm.HasTwoPrevious)
            {
                _rvm.CurrentIndex -= _pageSize;
            }
            else
            {
                _rvm.CurrentIndex--;
            }
            PopulatePreviousBox(_rvm.GetFile);
            if (_rvm.HasNext) PopulateNextBox(_rvm.GetFileForward);
            IndexLabel.Content = $"{_rvm.CurrentIndex + 1}/{_rvm.TotalFiles}";

        }
        private void GoForward(int moveNumber)
        {
            if (moveNumber == 0)
            {
                PopulatePreviousBox(_rvm.GetFile);
                if (_rvm.HasNext) PopulateNextBox(_rvm.GetFileForward);
            }
            else if (moveNumber == 1)
            {
                _rvm.CurrentIndex++;
            }
            else if (_rvm.FilesAhead >= 3)
            {
                _rvm.CurrentIndex += _pageSize;
            }
            else
            {
                _rvm.CurrentIndex++;
            }
            PopulatePreviousBox(_rvm.GetFile);
            if (_rvm.HasNext) PopulateNextBox(_rvm.GetFileForward);
            IndexLabel.Content = $"{_rvm.CurrentIndex+1}/{_rvm.TotalFiles}";

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
            if (_rvm == null) return;
            if (e.Delta < 0)
            {
                if (_rvm.FilesAhead >= 2)
                {
                    GoForward(CtrlIsDown() ? 1 : _pageSize);
                }
            }
            else if (e.Delta > 0)
            {
                if (_rvm.HasPrevious)
                {
                    GoBack(CtrlIsDown() ? 1 : _pageSize);
                }
            }
        }
    }
}

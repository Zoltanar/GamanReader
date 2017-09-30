using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GamanReader.Model;
using SevenZip;
using WpfAnimatedGif;

namespace GamanReader.ViewModel
{
	internal abstract class ContainerViewModel
	{
		private MainViewModel _mainModel;
		public ContainerViewModel(MainViewModel mainModel)
		{
			_mainModel = mainModel;
		}
		
		public string ContainerPath { get; protected set; }
		public int CurrentIndex { get; set; }
		public int TotalFiles { get; protected set; }
		protected string[] FileNames { get; set; }
		public int FilesAhead => TotalFiles - (CurrentIndex + 1);
		public bool HasNext => TotalFiles - (CurrentIndex + 1) > 0;
		public bool HasPrevious => CurrentIndex > 0;
		public bool HasTwoPrevious => CurrentIndex > 1;
		public abstract string GetFile(int index);
		public abstract bool IsFolder { get; }

		protected readonly List<string> RecognizedExtensions = new List<string>();
		protected bool FileIsImage(string filename)
		{
			var ext = Path.GetExtension(filename);
			return RecognizedExtensions.Exists(x =>
			{
				var extension = Path.GetExtension(x);
				return extension != null && extension.Equals(ext);
			});
		}


		public void GoBack(int moveNumber)
		{
			if (CurrentIndex == 0) return;
			if (moveNumber == 1)
			{
				CurrentIndex--;
			}
			else if (HasTwoPrevious)
			{
				CurrentIndex -= _mainModel.PageSize;
			}
			else
			{
				CurrentIndex--;
			}
			PopulatePreviousBox(CurrentIndex);
			if (_mainModel.PageSize == 2) PopulateNextBox(HasNext ? CurrentIndex+1 : -1);
			_mainModel.GoToIndexText = (CurrentIndex + 1).ToString();
			_mainModel.IndexLabelText = $"/{TotalFiles}";

		}

		public void GoForward(int moveNumber)
		{
			if (CurrentIndex == TotalFiles - 1) return;
			if (moveNumber == 0)
			{
				PopulatePreviousBox(CurrentIndex);
				if (_mainModel.PageSize == 2) PopulateNextBox(HasNext ? CurrentIndex+1 : -1);
			}
			else if (moveNumber == 1)
			{
				CurrentIndex++;
			}
			else if (FilesAhead >= 3)
			{
				CurrentIndex += _mainModel.PageSize;
			}
			else
			{
				CurrentIndex++;
			}
			PopulatePreviousBox(CurrentIndex);
			if (_mainModel.PageSize == 2) PopulateNextBox(HasNext ? CurrentIndex+1 : -1);
			_mainModel.GoToIndexText = (CurrentIndex + 1).ToString();
			_mainModel.IndexLabelText = $"/{TotalFiles}";

		}

		public void PopulatePreviousBox(int index)
		{
			ImageBox imagebox;
			if (_mainModel.PageSize == 1) imagebox = ImageBox.Single;
			else imagebox = _mainModel.RtlIsChecked.Value ? ImageBox.Right : ImageBox.Left;
			_mainModel.PopulateBox(imagebox, index);
		}

		public void PopulateNextBox(int index)
		{
			if (_mainModel.PageSize == 1) return;
			ImageBox imagebox;
			if (_mainModel.PageSize == 1) imagebox = ImageBox.Single;
			else imagebox = _mainModel.RtlIsChecked.Value ? ImageBox.Left : ImageBox.Right;
			_mainModel.PopulateBox(imagebox, index);
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

		/// <summary>
		/// Method to be called after object is created.
		/// </summary>
		internal void Initialize()
		{
			PopulatePreviousBox(CurrentIndex);
			PopulateNextBox(TotalFiles > 1 ? CurrentIndex+1 : -1);
		}
	}

	public enum ImageBox
	{
		Single = 0,
		Left = 1,
		Right = 2
	}

	/// <summary>
	/// Model that contains details about currently opened folder/file.
	/// </summary>
	internal class FolderViewModel : ContainerViewModel
	{
		public FolderViewModel(string containerPath, IEnumerable<string> fileNames, MainViewModel mainModel) : base(mainModel)
		{
			ContainerPath = containerPath;
			CurrentIndex = 0;
			foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
				RecognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(';'));
			RecognizedExtensions.Add("*.gif");
			FileNames = fileNames.Where(FileIsImage).ToArray();
			TotalFiles = FileNames.Length;
		}

		public override bool IsFolder => true;

		public override string GetFile(int index) => index == -1 ? null : FileNames[index];
	}



	/// <summary>
	/// Model that contains details about currently opened archive (supports zip and rar).
	/// </summary>
	internal class ArchiveViewModel : ContainerViewModel
	{
		public ArchiveViewModel(string containerPath, IEnumerable<string> fileNames, MainViewModel mainModel) : base(mainModel)
		{
			ContainerPath = containerPath;
			CurrentIndex = 0;
			foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
				RecognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(';'));
			FileNames = fileNames.Where(FileIsImage).ToArray();
			TotalFiles = FileNames.Length;
			_zipExtractor = new SevenZipExtractor(containerPath);
		}

		private readonly SevenZipExtractor _zipExtractor;

		public override bool IsFolder => false;

		public override string GetFile(int index)
		{
			if (index == -1) return null;
			var filename = FileNames[index];
			var tempFile = Path.Combine(StaticHelpers.TempFolder, filename);
			var fullPath = Path.GetFullPath(tempFile);
			if (File.Exists(tempFile)) return fullPath;
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			using (var stream = File.OpenWrite(tempFile))
			{
				_zipExtractor.ExtractFile(filename, stream);
			}
			return fullPath;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using SevenZip;
using static GamanReader.StaticHelpers;
using Newtonsoft.Json;
using System.Windows.Controls;

namespace GamanReader
{
	internal abstract class ViewModel
	{
		public string ContainerPath { get; protected set; }
		public int CurrentIndex { get; set; }
		public int TotalFiles { get; protected set; }
		public string[] FileNames { get; protected set; }
		public int FilesAhead => TotalFiles - (CurrentIndex + 1);
		public bool HasNext => TotalFiles - (CurrentIndex + 1) > 0;
		public bool HasPrevious => CurrentIndex > 0;
		public bool HasTwoPrevious => CurrentIndex > 1;
		public abstract string GetFile();
		public abstract string GetFileForward();

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

	}

	/// <summary>
	/// Model that contains details about currently opened folder/file.
	/// </summary>
	internal class FolderViewModel : ViewModel
	{
		public FolderViewModel(string containerPath, IEnumerable<string> fileNames)
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

		public override string GetFile() => FileNames[CurrentIndex];

		public override string GetFileForward() => FileNames[CurrentIndex + 1];
	}



	/// <summary>
	/// Model that contains details about currently opened archive (supports zip and rar).
	/// </summary>
	internal class ArchiveViewModel : ViewModel
	{
		public ArchiveViewModel(string containerPath, IEnumerable<string> fileNames)
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

		public override string GetFile()
		{
			var filename = FileNames[CurrentIndex];
			var tempFile = Path.Combine(TempFolder, filename);
			var fullPath = Path.GetFullPath(tempFile);
			if (File.Exists(tempFile)) return fullPath;
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
			using (var stream = File.OpenWrite(tempFile))
			{
				_zipExtractor.ExtractFile(filename, stream);
			}
			return fullPath;
		}

		public override string GetFileForward()
		{
			var filename = FileNames[CurrentIndex + 1];
			var tempFile = Path.Combine(TempFolder, filename);
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

﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SevenZip;

namespace GamanReader.Model
{
	/// <summary>
	/// Model that contains details about currently opened archive (supports zip and rar).
	/// </summary>
	internal class ArchiveContainer : Container
	{
		public ArchiveContainer(string containerPath, IEnumerable<string> fileNames)
		{
			ContainerPath = containerPath;
			CurrentIndex = 0;
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
			string generatedFolder = Path.Combine(StaticHelpers.TempFolder, ContainerPath.GetHashCode().ToString());
			var ext = Path.GetExtension(filename);
			var tempFile = Path.Combine(generatedFolder, index + ext);
			var fullPath = Path.GetFullPath(tempFile);
			if (File.Exists(tempFile)) return fullPath;
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new DirectoryNotFoundException($"Directory not found for path {fullPath}"));
			try
			{
				using (var stream = File.OpenWrite(tempFile))
				{
					_zipExtractor.ExtractFile(filename, stream);
				}
			}
			catch (System.Exception ex)
			{
				Debug.WriteLine($"Eror in ArchiveContainer.GetFile - {ex.Message}");
				return File.Exists(tempFile) ? fullPath : Path.GetFullPath(StaticHelpers.LoadFailedImage);
			}
			return fullPath;
		}

		public override void Dispose()
		{
			_zipExtractor?.Dispose();
		}
	}
}
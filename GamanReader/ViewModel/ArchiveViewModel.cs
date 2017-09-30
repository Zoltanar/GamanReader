using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using GamanReader.Model;
using SevenZip;

namespace GamanReader.ViewModel
{
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
			Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? throw new DirectoryNotFoundException($"Directory not found for path {fullPath}"));
			using (var stream = File.OpenWrite(tempFile))
			{
				_zipExtractor.ExtractFile(filename, stream);
			}
			return fullPath;
		}
	}
}
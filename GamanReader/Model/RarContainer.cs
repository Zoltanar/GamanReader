using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SevenZip;

namespace GamanReader.Model
{
	class RarContainer : ArchiveContainer
	{
		public RarContainer(string containerPath) : base(containerPath)
		{
			_rarExtractor = new SevenZipExtractor(containerPath);
			var files = _rarExtractor.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af => af.FileName).ToArray();
			FileNames = files.Where(FileIsImage).ToArray();
			if (new FileInfo(containerPath).Length > 40 * 1024 * 1024) ExtractAll();
		}

		private void ExtractAll()
		{
			for (int index = 0; index < TotalFiles; index++)
			{
				GetFile(index);
			}
		}

		private readonly SevenZipExtractor _rarExtractor;

		public override string GetFile(int index)
		{
			if (index == -1) return null;
			var filename = FileNames[index];
			var ext = Path.GetExtension(filename);
			var tempFile = Path.Combine(GeneratedFolder, index + ext);
			var fullPath = Path.GetFullPath(tempFile);
			if (File.Exists(tempFile)) return fullPath;
			try
			{
				using (var stream = File.OpenWrite(tempFile))
				{
					_rarExtractor.ExtractFile(filename, stream);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Eror in RarContainer.GetFile - {ex.Message}");
				return File.Exists(tempFile) ? fullPath : Path.GetFullPath(StaticHelpers.LoadFailedImage);
			}
			return fullPath;
		}

		public override void Dispose() => _rarExtractor.Dispose();

	}
}

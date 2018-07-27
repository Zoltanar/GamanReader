using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GamanReader.Model.Database;
using SevenZip;

namespace GamanReader.Model
{
	class RarContainer : ArchiveContainer
	{
		public RarContainer(MangaInfo item, Action onPropertyChanged) : base(item, onPropertyChanged)
		{
			_rarExtractor = new SevenZipExtractor(ContainerPath);
			var files = _rarExtractor.ArchiveFileData.OrderBy(entry => entry.FileName).Select(af => af.FileName).ToArray();
			FileNames = files.Where(FileIsImage).ToArray();
			if (new FileInfo(ContainerPath).Length > 40 * 1024 * 1024) ExtractAll();
		}

		private void ExtractAll()
		{
			for (int index = 0; index < TotalFiles; index++)
			{
				GetFile(index, out _);
			}
		}

		private readonly SevenZipExtractor _rarExtractor;

		public override string GetFile(int index, out string displayName)
		{
			displayName = null;
			if (index == -1) return null;
			var filename = FileNames[index];
			displayName = Path.GetFileName(filename);
			string hashedFilename = filename.GetHashCode().ToString();
			if (filename.Contains('\\'))
			{
				var folders = filename.Split('\\');
				hashedFilename = Path.Combine(folders.Select(t => t.GetHashCode().ToString()).ToArray());
			}
			var tempFile = Path.Combine(GeneratedFolder, hashedFilename);
			var fullPath = Path.GetFullPath(tempFile);
			Directory.CreateDirectory(Directory.GetParent(fullPath).FullName);
			
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
			finally
			{
				Extracted++;
				UpdateExtracted.Invoke();
			}
			return fullPath;
		}

		public override void Dispose() => _rarExtractor.Dispose();

	}
}

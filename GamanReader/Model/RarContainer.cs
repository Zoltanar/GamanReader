using System;
using System.ComponentModel;
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
			var bg = new BackgroundWorker();
			bg.DoWork += ExtractAllWork;
			bg.ProgressChanged += (sender, args) => onPropertyChanged();
			bg.WorkerReportsProgress = true;
			var rarExtractor = new SevenZipExtractor(ContainerPath);
			var fileNames = OrderFiles(rarExtractor.ArchiveFileData.Select(af => af.FileName), out var usingIntegers);
			if (false && !usingIntegers) fileNames = rarExtractor.ArchiveFileData.OrderBy(e => e.LastWriteTime).Select(f => f.FileName).ToArray();
			FileNames = fileNames;

			bg.RunWorkerAsync();
		}

		private void ExtractAllWork(object sender, DoWorkEventArgs e)
		{
			var rarExtractor = new SevenZipExtractor(ContainerPath);
			for (int index = 0; index < TotalFiles; index++)
			{
				GetFile(index, out _, rarExtractor, sender as BackgroundWorker);
			}
			rarExtractor.Dispose();
		}

		public override string GetFile(int index, out string displayName)
		{
			var extractor = new SevenZipExtractor(ContainerPath);
			try
			{
				return GetFile(index, out displayName, extractor, null);
			}
			finally
			{
				extractor.Dispose();
			}
		}
		public string GetFile(int index, out string displayName, SevenZipExtractor extractor, BackgroundWorker worker)
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
			try
			{
				if (File.Exists(tempFile)) return fullPath;
				using (var stream = File.OpenWrite(tempFile))
				{
					lock (this)
					{
						extractor.ExtractFile(filename, stream);
					}

				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in RarContainer.GetFile - {ex.Message}");
				return File.Exists(tempFile) ? fullPath : Path.GetFullPath(StaticHelpers.LoadFailedImage);
			}
			finally
			{
				worker?.ReportProgress(Extracted);
				Extracted++;
				UpdateExtracted.Invoke();
			}
			return fullPath;
		}

		public override void Dispose() { }

	}
}

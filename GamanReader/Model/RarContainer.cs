using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using SevenZip;

namespace GamanReader.Model
{
	internal class RarContainer : ArchiveContainer<ArchiveFileInfo>
	{
		public RarContainer(MangaInfo item, Action<string> onPropertyChanged, MainViewModel.PageOrder pageOrder, bool extractFirstOnly = false) : base(item, onPropertyChanged, pageOrder)
		{
			using var rarExtractor = new SevenZipExtractor(ContainerPath);
			OrderFiles(rarExtractor.ArchiveFileData);
			if (extractFirstOnly)
			{
				ExtractFirst();
			}
			else
			{
				var bg = new BackgroundWorker();
				bg.DoWork += ExtractAllWork;
				bg.ProgressChanged += (_, _) => onPropertyChanged?.Invoke(nameof(Extracted));
				bg.WorkerReportsProgress = true;
				bg.RunWorkerAsync();
			}
		}


		protected override IEnumerable<string> OrderFilesByDateModified(IEnumerable<ArchiveFileInfo> files)
		{
			return files.OrderBy(e => e.LastWriteTime).Select(f => f.FileName);
		}

		protected override IEnumerable<string> GetFileNames(IEnumerable<ArchiveFileInfo> files)
		{
			return files.OrderBy(c => c.FileName).Select(c=>c.FileName);
		}

		public static int GetFileCount(string containerPath)
		{
			var rarExtractor = new SevenZipExtractor(containerPath);
			return rarExtractor.ArchiveFileData.Select(af => af.FileName).Count(FileIsImage);
		}
		
		private void ExtractAllWork(object sender, DoWorkEventArgs e)
		{
			if (TotalFiles == 0) return;
			var rarExtractor = new SevenZipExtractor(ContainerPath);
			for (int index = 0; index < TotalFiles; index++)
			{
				GetFile(index, out _, rarExtractor, sender as BackgroundWorker);
			}
			rarExtractor.Dispose();
		}
		private void ExtractFirst()
		{
			if (TotalFiles == 0) return;
			var tempFile = GetFileFromUnorderedIndex(0);
			if (File.Exists(tempFile)) return;
			using var fileStream = File.OpenWrite(tempFile);
			var rarExtractor = new SevenZipExtractor(ContainerPath);
			rarExtractor.ExtractFile(FileNames[0], fileStream);
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
			GetFileFromOrderedIndex(index, out var archivePath, out displayName, out var tempFile);
			var fullPath = Path.GetFullPath(tempFile);
			Directory.CreateDirectory(Directory.GetParent(fullPath).FullName);
			try
			{
				if (File.Exists(tempFile)) return fullPath;
				using var stream = File.OpenWrite(tempFile);
				lock (this)
				{
					extractor.ExtractFile(archivePath, stream);
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
				UpdateExtracted?.Invoke(nameof(Extracted));
			}
			return fullPath;
		}
		
		public override void Dispose() { }

	}
}

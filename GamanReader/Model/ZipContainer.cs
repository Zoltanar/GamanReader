using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GamanReader.Model.Database;
using GamanReader.ViewModel;

namespace GamanReader.Model
{
	internal class ZipContainer : ArchiveContainer<ZipArchiveEntry>
	{
		public ZipContainer(MangaInfo item, Action<string> onPropertyChanged, MainViewModel.PageOrder pageOrder) : base(item, onPropertyChanged, pageOrder)
		{
			using var archive = new ZipArchive(File.OpenRead(ContainerPath));
			OrderFiles(archive.Entries);
		}

		public static int GetFileCount(string containerPath)
		{
			using var archive = new ZipArchive(File.OpenRead(containerPath));
			return archive.Entries.Select(af => af.Name).Count(FileIsImage);
		}

		public async Task ExtractAllAsync(CancellationToken token)
		{
			var file = new FileInfo(ContainerPath);
			var extensions = new[] { ".zip", ".cbz" };
			if (!extensions.Contains(file.Extension)) return;
			await Task.Run(() =>
			{
				using var archive = new ZipArchive(File.OpenRead(ContainerPath));
				var entries = FileNames.Select(f => archive.Entries.First(e => e.FullName == f)).ToList();
				for (var index = 0; index < entries.Count; index++)
				{
					var entry = entries[index];
					if (Extracted > index) continue;
					if (token.IsCancellationRequested) return;
					try
					{
						var tempFile = GetFileFromUnorderedIndex(index);
						if (File.Exists(tempFile)) continue;
						var fileStream = File.OpenWrite(tempFile);
						var zipStream = entry.Open();
						zipStream.CopyTo(fileStream);
						zipStream.Dispose();
						fileStream.Dispose();
					}
					finally
					{
						Extracted++;
						UpdateExtracted?.Invoke(nameof(Extracted));
					}
				}
			}, token);
		}

		public override string GetFile(int index, out string displayName)
		{
			var watch = Stopwatch.StartNew();
			displayName = null;
			if (index == -1) return null;
			GetFileFromOrderedIndex(index, out _, out displayName, out var tempFile);
			var fullPath = Path.GetFullPath(tempFile);
			while (Extracted < index && watch.Elapsed.TotalSeconds < 10) Thread.Sleep(250);
			return fullPath;
		}

		public override void Dispose()
		{
			//nothing needed for this archive type
		}

		protected override IEnumerable<string> OrderFilesByDateModified(IEnumerable<ZipArchiveEntry> files)
		{
			return files.OrderBy(e => e.LastWriteTime).Select(f => f.FullName);
		}

		protected override IEnumerable<string> GetFileNames(IEnumerable<ZipArchiveEntry> files)
		{
			return files.Select(f => f.FullName);
		}

		public void ExtractFirstItem()
		{
			if (Extracted >= 1)
			{
				return;
			}
			if (FileNames.Length == 0) return;
			var file = new FileInfo(ContainerPath);
			var extensions = new[] { ".zip", ".cbz" };
			if (!extensions.Contains(file.Extension)) throw new NotSupportedException($"Extension not supported: {file.Extension}");
			using var archive = new ZipArchive(File.OpenRead(ContainerPath));
			var entry = archive.Entries.First(e => e.FullName == FileNames[0]);
			var tempFile = GetFileFromUnorderedIndex(0);
			if (File.Exists(tempFile)) return;
			using var fileStream = File.OpenWrite(tempFile);
			using var zipStream = entry.Open();
			zipStream.CopyTo(fileStream);
		}
	}
}

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GamanReader.Model.Database;

namespace GamanReader.Model
{
	class ZipContainer : ArchiveContainer
	{
		public ZipContainer(MangaInfo item, Action onPropertyChanged) : base(item, onPropertyChanged)
		{
			using (var archive = new ZipArchive(File.OpenRead(ContainerPath)))
			{
				FileNames = OrderFiles(archive.Entries.Select(af => af.Name));
			}
		}

		public async Task ExtractAllAsync(CancellationToken token)
		{
			var file = new FileInfo(ContainerPath);
			var extensions = new[] {".zip", ".cbz"};
			if (!extensions.Contains(file.Extension)) return; //extract all if file is less than 40mb
			await Task.Run(() =>
			{
				using var archive = new ZipArchive(File.OpenRead(ContainerPath));
				var entries = FileNames.Select(f => archive.Entries.First(e => e.Name == f)).ToList();
				foreach (var entry in entries)
				{
					if (token.IsCancellationRequested) return;
					try
					{
						var tempFile = Path.Combine(GeneratedFolder, entry.Name);
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
						UpdateExtracted.Invoke();
					}
				}
			}, token);
		}

		public override string GetFile(int index, out string displayName)
		{
			displayName = null;
			if (index == -1) return null;
			var filename = FileNames[index];
			displayName = Path.GetFileName(filename);
			var tempFile = Path.Combine(GeneratedFolder, filename);
			var fullPath = Path.GetFullPath(tempFile);
			while (Extracted < index) Thread.Sleep(250);
			return fullPath;
		}

		public override void Dispose()
		{
			//nothing needed for this archive type
		}
	}
}

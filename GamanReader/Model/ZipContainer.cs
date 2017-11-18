using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GamanReader.Model
{
	class ZipContainer : ArchiveContainer
	{
		public ZipContainer(string containerPath, Action onPropertyChanged) : base(containerPath, onPropertyChanged)
		{
			using (var archive = new ZipArchive(File.OpenRead(ContainerPath)))
			{
				FileNames = archive.Entries.OrderBy(e => e.Name).Where(f => FileIsImage(f.Name)).Select(x => x.Name).ToArray();
				var namesWithoutExtension = FileNames.Select(Path.GetFileNameWithoutExtension).ToArray();
				// ReSharper disable once AssignNullToNotNullAttribute
				if (namesWithoutExtension.All(x => int.TryParse(x, out _))) FileNames = FileNames.OrderBy(x=>int.Parse(Path.GetFileNameWithoutExtension(x))).ToArray();
			}
		}

		public async Task ExtractAllAsync()
		{
			var file = new FileInfo(ContainerPath);
			if (!file.Extension.Equals(".zip")) return; //extract all if file is less than 40mb
			await Task.Run(() =>
			{
				var archive = new ZipArchive(File.OpenRead(ContainerPath));
				var entries = archive.Entries.OrderBy(e => e.Name).Where(f => FileIsImage(f.Name)).ToArray();
				var namesWithoutExtension = entries.Select(x=> Path.GetFileNameWithoutExtension(x.Name)).ToArray();
				// ReSharper disable once AssignNullToNotNullAttribute
				if (namesWithoutExtension.All(x => int.TryParse(x, out _))) entries = entries.OrderBy(x => int.Parse(Path.GetFileNameWithoutExtension(x.Name))).ToArray();
				foreach (var entry in entries)
				{
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
				archive.Dispose();
			});
		}

		public override string GetFile(int index)
		{
			if (index == -1) return null;
			var filename = FileNames[index];
			var tempFile = Path.Combine(GeneratedFolder, filename);
			var fullPath = Path.GetFullPath(tempFile);
			while (Extracted <= index) Thread.Sleep(250);
			return fullPath;
		}

		public override void Dispose()
		{
			//nothing needed for this archive type
		}
	}
}

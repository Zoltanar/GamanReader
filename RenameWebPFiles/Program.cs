using System;
using System.IO;
using System.Text;

namespace RenameWebPFiles
{
	class Program
	{
		static int fileRenamed = 0;
		static int actualJpgFiles = 0;

		static void Main(string[] args)
		{
			if (args.Length == 0) return;
			var directory = new DirectoryInfo(args[0]);
			if (!directory.Exists) return;
			RenameFiles(directory);
		}

		private static void RenameFiles(DirectoryInfo directory)
		{
			foreach (var directoryInfo in directory.EnumerateDirectories())
			{
				RenameFiles(directoryInfo);
			}
			foreach (var file in directory.EnumerateFiles("*.jpg"))
			{
				try
				{
					if (file.Extension != ".jpg") continue;
					using (var f = File.OpenRead(file.FullName))
					{
						var bytes16 = new byte[16];
						var numRead = f.Read(bytes16, 0, 16);
						if (numRead < 16) continue;
						var utf8 = Encoding.UTF8.GetString(bytes16);
						if (!utf8.Contains("WEBP"))
						{
							actualJpgFiles++;
							continue;
						}
					}
					var newFile = Path.Combine(file.DirectoryName,Path.GetFileNameWithoutExtension(file.FullName) + ".webp");
					fileRenamed++;
					file.MoveTo(newFile);
				}
				catch (Exception ex)
				{
					throw;
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GamanReader.Model
{
	public static class RandomFile
	{
		private static readonly Random Rand = new Random();

		#region GetRandomFile
		private static string _folder;
		private static bool _includeSubdirectories;
		private static bool _compatibleOnly;

		private static string Folder
		{
			get => _folder; set
			{
				if (_folder == value) return;
				_folder = value;
				_folderChanged = true;
			}
		}
		private static bool IncludeSubdirectories
		{
			get => _includeSubdirectories; set
			{
				if (_includeSubdirectories == value) return;
				_includeSubdirectories = value;
				_folderChanged = true;
			}
		}
		private static bool CompatibleOnly
		{
			get => _compatibleOnly;
			set
			{
				if (_compatibleOnly == value) return;
				_compatibleOnly = value;
				_folderChanged = true;
			}
		}
		private static string[] _files;
		private static bool _folderChanged;

		public static string GetRandomFile(string folder, bool includeSubdirectories, bool compatibleOnly, out string error)
		{
			error = "No Error.";
			Folder = folder;
			IncludeSubdirectories = includeSubdirectories;
			CompatibleOnly = compatibleOnly;
			if (string.IsNullOrWhiteSpace(folder))
			{
				error = "Folder was empty.";
				return null;
			}
			if (_folderChanged)
			{
				IEnumerable<string> exts;
				if (compatibleOnly)
				{
					exts = StaticHelpers.AllowedFormats.Select(i => $"*.{i}");
				}
				else exts = new[] { "*.*" };
				var option = (SearchOption)(IncludeSubdirectories ? 1 : 0);
				_files = exts.SelectMany(x => Directory.EnumerateFiles(Folder, x, option)).ToArray();
				if (_files.Length == 0)
				{
					error = $"No files found.";
					return null;
				}
			}
			var randomFile = _files[Rand.Next(_files.Length)];
			return randomFile;
		}
		#endregion

		#region GetRandomFileOrFolder

		private static string _fileOrFolderPath;
		private static bool _fileOrFolderPathChanged;

		private static string FileOrFolderPath
		{
			get => _fileOrFolderPath;
			set
			{
				if (_fileOrFolderPath == value) return;
				_fileOrFolderPath = value;
				_fileOrFolderPathChanged = true;
			}
		}
		private static (string Path, bool IsFolder)[] _filesOrFolders;

		public static string GetRandomFileOrFolder(string folder, out bool isFolder, out string error)
		{
			error = "No error.";
			isFolder = false;
			FileOrFolderPath = folder;
			try
			{
				if (_fileOrFolderPathChanged)
				{
					var exts = StaticHelpers.AllowedFormats.Select(i => $"*.{i}");
					var files = exts.SelectMany(x => Directory.EnumerateFiles(FileOrFolderPath, x)).ToArray();
					var folders = Directory.GetDirectories(FileOrFolderPath);
					var list = new List<(string Path, bool IsFolder)>();
					foreach (var item in files) list.Add((item, false));
					foreach (var item in folders) list.Add((item, true));
					_filesOrFolders = list.ToArray();
				}
				if(_filesOrFolders.Length == 0)
				{
					error = "No files or folders found.";
					return null;
				}
				var randomFileOrFolder = _filesOrFolders[Rand.Next(_filesOrFolders.Length)];
				isFolder = randomFileOrFolder.IsFolder;
				return randomFileOrFolder.Path;
			}
			catch(Exception ex)
			{
				error = ex.Message;
				return null;
			}
		}

		#endregion

	}
}

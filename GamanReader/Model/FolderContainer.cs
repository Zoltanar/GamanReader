using System.Collections.Generic;
using System.IO;
using System.Linq;
using GamanReader.Model.Database;
using GamanReader.ViewModel;

namespace GamanReader.Model
{
	/// <summary>
	/// Model that contains details about currently opened folder/file.
	/// </summary>
	internal class FolderContainer : Container<FileInfo>
	{
		public FolderContainer(MangaInfo item, IEnumerable<FileInfo> files, MainViewModel.PageOrder pageOrder) : base(item, pageOrder)
		{
			CurrentIndex = 0;
			OrderFiles(files);
		}

		public override bool IsFolder => true;

		public override void Dispose()
		{
			//not needed for folder
		}

		protected override IEnumerable<string> OrderFilesByDateModified(IEnumerable<FileInfo> files)
		{
			return files
				.OrderBy(f => f.LastWriteTimeUtc)
				.Select(f => f.FullName);
		}

		protected override IEnumerable<string> GetFileNames(IEnumerable<FileInfo> files)
		{
			return files.Select(f => f.FullName);
		}

		public override string FileDirectory => ContainerPath;

		public override string GetFile(int index, out string displayName)
		{
			displayName = null;
			if (index == -1) return null;
			var filename = OrderedFileNames[index];
			displayName = filename.Remove(0, ContainerPath.Length+1);
			return filename;
		}
	}
}
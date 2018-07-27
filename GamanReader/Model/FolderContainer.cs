using System.Collections.Generic;
using System.IO;
using System.Linq;
using GamanReader.Model.Database;

namespace GamanReader.Model
{
	/// <summary>
	/// Model that contains details about currently opened folder/file.
	/// </summary>
	internal class FolderContainer : Container
	{
		public FolderContainer(MangaInfo item, IEnumerable<string> fileNames) : base(item)
		{
			CurrentIndex = 0;
			FileNames = fileNames.Where(FileIsImage).ToArray();
		}

		public override bool IsFolder => true;
		public override void Dispose()
		{
			//not needed for folder
		}

		public override string GetFile(int index, out string displayName)
		{
			displayName = null;
			if (index == -1) return null;
			var filename = FileNames[index];
			displayName = Path.GetFileName(filename);
			return filename;
		}
	}
}
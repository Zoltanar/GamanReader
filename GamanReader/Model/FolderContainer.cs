using System.Collections.Generic;
using System.Linq;

namespace GamanReader.Model
{
	/// <summary>
	/// Model that contains details about currently opened folder/file.
	/// </summary>
	internal class FolderContainer : Container
	{
		public FolderContainer(string containerPath, IEnumerable<string> fileNames)
		{
			ContainerPath = containerPath;
			CurrentIndex = 0;
			FileNames = fileNames.Where(FileIsImage).ToArray();
		}

		public override bool IsFolder => true;
		public override void Dispose()
		{
			//not needed for folder
		}

		public override string GetFile(int index) => index == -1 ? null : FileNames[index];
	}
}
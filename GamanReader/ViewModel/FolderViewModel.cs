using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;

namespace GamanReader.ViewModel
{
	/// <summary>
	/// Model that contains details about currently opened folder/file.
	/// </summary>
	internal class FolderViewModel : ContainerViewModel
	{
		public FolderViewModel(string containerPath, IEnumerable<string> fileNames, MainViewModel mainModel) : base(mainModel)
		{
			ContainerPath = containerPath;
			CurrentIndex = 0;
			foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
				RecognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(';'));
			RecognizedExtensions.Add("*.gif");
			FileNames = fileNames.Where(FileIsImage).ToArray();
			TotalFiles = FileNames.Length;
		}

		public override bool IsFolder => true;

		public override string GetFile(int index) => index == -1 ? null : FileNames[index];
	}
}
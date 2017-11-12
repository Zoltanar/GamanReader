using System;
using System.IO;

namespace GamanReader.Model
{
	/// <summary>
	/// Model that contains details about currently opened archive (supports zip and rar).
	/// </summary>
	internal abstract class ArchiveContainer : Container
	{
		protected ArchiveContainer(string containerPath, Action onPropertyChanged)
		{
			ContainerPath = containerPath;
			CurrentIndex = 0;
			GeneratedFolder = Path.Combine(StaticHelpers.TempFolder, ContainerPath.GetHashCode().ToString());
			Directory.CreateDirectory(GeneratedFolder);
			UpdateExtracted = onPropertyChanged;
		}

		protected readonly string GeneratedFolder;

		public override bool IsFolder => false;
	}
}
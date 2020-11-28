using System;
using System.IO;
using GamanReader.Model.Database;
using GamanReader.ViewModel;

namespace GamanReader.Model
{
	/// <summary>
	/// Model that contains details about currently opened archive (supports zip and rar).
	/// </summary>
	internal abstract class ArchiveContainer<T> : Container<T>
	{
		protected ArchiveContainer(MangaInfo item, Action<string> onPropertyChanged, MainViewModel.PageOrder pageOrder) : base(item, pageOrder)
		{
			CurrentIndex = 0;
			GeneratedFolder = Path.Combine(StaticHelpers.TempFolder, ContainerPath.GetHashCode().ToString());
			Directory.CreateDirectory(GeneratedFolder);
			UpdateExtracted = onPropertyChanged;
		}

		protected readonly string GeneratedFolder;

		public override bool IsFolder => false;
	}
}
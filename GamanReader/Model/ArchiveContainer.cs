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
		public sealed override string FileDirectory { get; }

		public override bool IsFolder => false;
		
		protected ArchiveContainer(MangaInfo item, Action<string> onPropertyChanged, MainViewModel.PageOrder pageOrder) : base(item, pageOrder)
		{
			FileDirectory = Path.Combine(StaticHelpers.TempFolder, ContainerPath.GetHashCode().ToString());
			Directory.CreateDirectory(FileDirectory);
			UpdateExtracted = onPropertyChanged;
		}
		
		/// <summary>
		/// Gets information for the file indicated by the index of ordered files.
		/// </summary>
		/// <param name="orderedIndex">Index of file to get (from user-ordered list)</param>
		/// <param name="archivePath">Full path of file in archive</param>
		/// <param name="displayName">Display name of file</param>
		/// <param name="tempFile">Temporary file created for file</param>
		protected void GetFileFromOrderedIndex(int orderedIndex, out string archivePath, out string displayName, out string tempFile)
		{
			archivePath = OrderedFileNames[orderedIndex];
			var actualIndex = Array.IndexOf(FileNames, archivePath);
			displayName = Path.GetFileName(archivePath);
			tempFile = Path.Combine(FileDirectory, $"{actualIndex}{Path.GetExtension(archivePath)}");
		}
	}
}
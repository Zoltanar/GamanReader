using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using JetBrains.Annotations;

namespace GamanReader.Model
{
	public abstract class Container<T> : Container
	{
		protected Container(MangaInfo item, MainViewModel.PageOrder pageOrder) : base(item, pageOrder) { }

		protected void OrderFiles(IEnumerable<T> files)
		{
			// ReSharper disable once PossibleMultipleEnumeration
			FileNames = GetFileNames(files).Where(FileIsImage).OrderBy(c => c).ToArray();
			OrderedFileNames = PageOrderMode switch
			{
				MainViewModel.PageOrder.Natural => GetFileNames(files)
					.Where(FileIsImage)
					.OrderBy(ef => ef, new NaturalSortComparer())
					.ToArray(),
				MainViewModel.PageOrder.Modified => OrderFilesByDateModified(files).Where(FileIsImage).ToArray(),
				MainViewModel.PageOrder.Ordinal => FileNames.ToArray(),
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		protected abstract IEnumerable<string> OrderFilesByDateModified(IEnumerable<T> files);

		protected abstract IEnumerable<string> GetFileNames(IEnumerable<T> files);

	}

	public abstract class Container : IDisposable
	{
		public static readonly HashSet<string> RecognizedExtensions = new(StringComparer.OrdinalIgnoreCase);

		static Container()
		{
			foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
			{
				foreach (var ext in imageCodec.FilenameExtension.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(Path.GetExtension))
					RecognizedExtensions.Add(ext);
			}
			RecognizedExtensions.Add(".gif");
		}

		public MangaInfo Item { get; }
		public string ContainerPath { get; }
		/// <summary>
		/// Either temporary directory with extracted files, or folder itself.
		/// </summary>
		public abstract string FileDirectory { get; }
		public MainViewModel.PageOrder PageOrderMode { get; }
		private int _pagesBrowsed;
		private bool _addedTimeBrowsed;
		public int CurrentIndex
		{
			get => _currentIndex;
			set
			{
				if (value == _currentIndex) return;
				_pagesBrowsed += StaticHelpers.NumberBetween(0, 2, value - _currentIndex);
				_currentIndex = value;
				if (_addedTimeBrowsed || _pagesBrowsed <= Math.Min(TotalFiles * 0.5, 15)) return;
				Item.TimesBrowsed++;
				_addedTimeBrowsed = true;
				StaticHelpers.MainModel.ViewModel.UpdateStats(true, Item.Length);

			}
		}

		public int TotalFiles => FileNames.Length;
		public string[] FileNames { get; protected set; }
		public string[] OrderedFileNames { get; protected set; }
		public abstract string GetFile(int index, out string displayName);
		public abstract bool IsFolder { get; }
		public int Extracted { get; protected set; }

		protected Action<string> UpdateExtracted;
		private int _currentIndex;

		protected Container(MangaInfo item, MainViewModel.PageOrder pageOrder)
		{
			Item = item;
			ContainerPath = item.FilePath;
			PageOrderMode = pageOrder;
			CurrentIndex = 0;
		}

		public static bool FileIsImage([NotNull] string filename)
		{
			return RecognizedExtensions.Contains(Path.GetExtension(filename).ToLower());
		}

		public abstract void Dispose();

		public class StringWithNumberComparer : IComparer<string>
		{
			//private static readonly Regex Pattern = new Regex(@"([a-z]+)([0-9]+)?([a-z]+)?");
			private static readonly Regex Pattern = new Regex(@"(\D+)(\d+)?(\D+)?");
			private static readonly Regex Pattern2 = new Regex(@"\d\d\d\d?.*");
			public int Compare(string x, string y)
			{
				if (x == null) return y == null ? 0 : 1;
				if (y == null) return -1;
				var dirParts1 = x.Split('\\').ToList();
				var dirParts2 = y.Split('\\').ToList();
				while (dirParts1[0] == dirParts2[0])
				{
					dirParts1.RemoveAt(0);
					dirParts2.RemoveAt(0);
					if (dirParts1.Count == 0 || dirParts2.Count == 0) return string.CompareOrdinal(x, y);
				}
				if (Pattern2.IsMatch(dirParts1[0]) && Pattern2.IsMatch(dirParts2[0])) return string.CompareOrdinal(x, y);
				var parts1 = Pattern.Match(dirParts1[0]);
				var parts2 = Pattern.Match(dirParts2[0]);
				if (parts1.Groups.Count <= 2 || parts2.Groups.Count <= 2 || parts1.Groups[1].Value != parts2.Groups[1].Value) return string.CompareOrdinal(x, y);
				return parts1.Groups[2].Value != parts2.Groups[2].Value ?
					int.Parse(parts1.Groups[2].Value).CompareTo(int.Parse(parts2.Groups[2].Value)) :
					string.CompareOrdinal(x, y);
			}
		}

		public string GetFileFromUnorderedIndex(int unorderedIndex)
		{
			var file = FileNames[unorderedIndex];
			return IsFolder ? file : Path.Combine(FileDirectory, $"{unorderedIndex}{Path.GetExtension(file)}");
		}
	}

}

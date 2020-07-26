using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GamanReader.Model.Database;
using JetBrains.Annotations;

namespace GamanReader.Model
{
	public abstract class Container : IDisposable
	{
		public MangaInfo Item { get;}
		public string ContainerPath { get; }
		int _pagesBrowsed;
		private bool _addedTimeBrowsed;
		public int CurrentIndex
		{
			get => _currentIndex;
			set
			{
				if (value == _currentIndex) return;
				_pagesBrowsed += StaticHelpers.NumberBetween(0, 2, value - _currentIndex);
		_currentIndex = value;
				if (_addedTimeBrowsed || _pagesBrowsed <= Math.Min(TotalFiles*0.5, 15)) return;
				Item.TimesBrowsed++;
				_addedTimeBrowsed = true;

			}
		}

		public int TotalFiles => FileNames.Length;
		public string[] FileNames { get; protected set; }
		public abstract string GetFile(int index, out string displayName);
		public abstract bool IsFolder { get; }
		public int Extracted { get; protected set; }

		protected Action UpdateExtracted;
		private int _currentIndex;

		private static readonly List<string> RecognizedExtensions = new List<string>();

		static Container()
		{
			foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
			{
				RecognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(';'));
			}
			RecognizedExtensions.Add("*.gif");
		}

		protected static bool FileIsImage([NotNull]string filename)
		{
			var ext = Path.GetExtension(filename).ToLower();
			return RecognizedExtensions.Exists(x =>
			{
				var extension = Path.GetExtension(x);
				return extension != null && extension.Equals(ext);
			});
		}

		protected Container(MangaInfo item)
		{
			Item = item;
			ContainerPath = item.FilePath;
		}

		public abstract void Dispose();

		protected static string[] OrderFiles(IEnumerable<string> fileNames, out bool usingIntegers)
		{
			//todo allow user to choose different modes
			//todo add mode int is parsed at the beginning
			var list = fileNames.ToList();
			var namesWithoutExtension = list.Select(Path.GetFileNameWithoutExtension).ToArray();
			// ReSharper disable once AssignNullToNotNullAttribute
			var minNonInts = list.Count * 0.25;//Math.Min(list.Count * 0.25, 4);
			var integers = namesWithoutExtension.Count(x => int.TryParse(x, out _));
			usingIntegers = integers > list.Count - minNonInts;
			if (usingIntegers)
			{
				return  list.OrderBy(x =>
				{
					var success = int.TryParse(Path.GetFileNameWithoutExtension(x), out var number);
					return success ? number : int.MaxValue;
				}).ToArray();
			}
			return list.Where(FileIsImage).OrderBy(ef => ef, new StringWithNumberComparer()).ToArray();
		}

		public class StringWithNumberComparer : IComparer<string>
		{
			//private static readonly Regex Pattern = new Regex(@"([a-z]+)([0-9]+)?([a-z]+)?");
			private static readonly Regex Pattern = new Regex(@"(\D+)(\d+)?(\D+)?");
			private static readonly Regex Pattern2 = new Regex(@"\d\d\d\d?.*");
			public int Compare(string x, string y)
			{
				if (x == null && y == null) return 0;
				if (x == null) return 1;
				if (y == null) return -1;
				var dirParts1 = x.Split('\\').ToList();
				var dirParts2 = y.Split('\\').ToList();
				while (dirParts1[0] == dirParts2[0])
				{
					dirParts1.RemoveAt(0);
					dirParts2.RemoveAt(0);
					if (dirParts1.Count == 0 || dirParts2.Count == 0) return string.CompareOrdinal(x, y);
				}
				if(Pattern2.IsMatch(dirParts1[0]) && Pattern2.IsMatch(dirParts2[0])) return string.CompareOrdinal(x, y);
				var parts1 = Pattern.Match(dirParts1[0]);
				var parts2 = Pattern.Match(dirParts2[0]);
				if (parts1.Groups.Count <= 2 || parts2.Groups.Count <= 2 || parts1.Groups[1].Value != parts2.Groups[1].Value) return string.CompareOrdinal(x, y);
				return parts1.Groups[2].Value != parts2.Groups[2].Value ?
					int.Parse(parts1.Groups[2].Value).CompareTo(int.Parse(parts2.Groups[2].Value)) :
					string.CompareOrdinal(x, y);
			}
		}
	}

}

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using GamanReader.Model.Database;
using JetBrains.Annotations;

namespace GamanReader.Model
{
	public abstract class Container : IDisposable
	{
		public MangaInfo Item { get;}
		public string ContainerPath { get; }
		int _pagesBrowsed;

		public int CurrentIndex
		{
			get => _currentIndex;
			set
			{
				if (value == _currentIndex) return;
				_currentIndex = value;
				_pagesBrowsed++;
				if (_pagesBrowsed == 5) Item.TimesBrowsed++;

			}
		}

		public int TotalFiles => FileNames.Length;
		protected string[] FileNames { get; set; }
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
	}

}

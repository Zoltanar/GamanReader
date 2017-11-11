using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using JetBrains.Annotations;

namespace GamanReader.Model
{
	public abstract class Container : IDisposable
	{
		protected string ContainerPath { get; set; }
		public int CurrentIndex { get; set; }
		public int TotalFiles => FileNames.Length;
		protected string[] FileNames { get; set; }
		public abstract string GetFile(int index);
		public abstract bool IsFolder { get; }
		public int Extracted { get; protected set; }

		protected Action UpdateExtracted;

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

		public abstract void Dispose();
	}

}

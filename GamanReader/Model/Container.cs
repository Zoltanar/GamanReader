using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;

namespace GamanReader.Model
{
	internal abstract class Container
	{
		public string ContainerPath { get; protected set; }
		public int CurrentIndex { get; set; }
		public int TotalFiles { get; protected set; }
		protected string[] FileNames { get; set; }
		public abstract string GetFile(int index);
		public abstract bool IsFolder { get; }

		private static readonly List<string> RecognizedExtensions = new List<string>();

		static Container()
		{
			foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
			{
				RecognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(';'));
			}
			RecognizedExtensions.Add("*.gif");
		}
		protected static bool FileIsImage(string filename)
		{
			var ext = Path.GetExtension(filename);
			return RecognizedExtensions.Exists(x =>
			{
				var extension = Path.GetExtension(x);
				return extension != null && extension.Equals(ext);
			});
		}
	}

}

using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace GamanReader
{
    /// <summary>
    /// Model that contains details about currently opened folder/file.
    /// </summary>
    internal class ReaderViewModel
    {
        public ReaderViewModel(string folderName, string[] fileNames)
        {
            FolderName = folderName;
            CurrentIndex = 0;
            foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
                _recognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(";".ToCharArray()));
            FileNames = fileNames.Where(FileIsImage).ToArray();
            TotalFiles = FileNames.Length;
        }

        public string FolderName { get; }
        public int CurrentIndex { get; set; }
        public int TotalFiles { get; }
        public string[] FileNames { get; }


        public int FilesAhead => TotalFiles - (CurrentIndex + 1);
        public bool HasNext => TotalFiles - (CurrentIndex + 1) > 0;
        public bool HasPrevious => CurrentIndex > 0;
        public bool HasTwoPrevious => CurrentIndex > 1;
        public string GetFile => FileNames[CurrentIndex];
        public string GetFileForward => FileNames[CurrentIndex + 1];

        private readonly List<string> _recognizedExtensions = new List<string>();
        private bool FileIsImage(string filename)
        {
            var ext = Path.GetExtension(filename);
            return _recognizedExtensions.Exists(x =>
            {
                var extension = Path.GetExtension(x);
                return extension != null && extension.Equals(ext);
            });
        }
    }
}

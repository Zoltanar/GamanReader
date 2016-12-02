using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using SevenZip;
using static GamanReader.StaticHelpers;

namespace GamanReader
{
    internal abstract class ViewModel
    {
        public string ContainerPath { get; protected set; }
        public int CurrentIndex { get; set; }
        public int TotalFiles { get; protected set; }
        public string[] FileNames { get; protected set; }
        public int FilesAhead => TotalFiles - (CurrentIndex + 1);
        public bool HasNext => TotalFiles - (CurrentIndex + 1) > 0;
        public bool HasPrevious => CurrentIndex > 0;
        public bool HasTwoPrevious => CurrentIndex > 1;
        public abstract string GetFile();
        public abstract string GetFileForward();

        protected readonly List<string> RecognizedExtensions = new List<string>();
        protected bool FileIsImage(string filename)
        {
            var ext = Path.GetExtension(filename);
            return RecognizedExtensions.Exists(x =>
            {
                var extension = Path.GetExtension(x);
                return extension != null && extension.Equals(ext);
            });
        }
    }

    /// <summary>
    /// Model that contains details about currently opened folder/file.
    /// </summary>
    internal class FolderViewModel : ViewModel
    {
        public FolderViewModel(string containerPath, IEnumerable<string> fileNames)
        {
            ContainerPath = containerPath;
            CurrentIndex = 0;
            foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
                RecognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(';'));
            FileNames = fileNames.Where(FileIsImage).ToArray();
            TotalFiles = FileNames.Length;
        }
        public override string GetFile() => FileNames[CurrentIndex];

        public override string GetFileForward() => FileNames[CurrentIndex + 1];
    }



    /// <summary>
    /// Model that contains details about currently opened folder/file.
    /// </summary>
    internal class ZipViewModel : ViewModel
    {
        public ZipViewModel(string containerPath, IEnumerable<string> fileNames)
        {
            ContainerPath = containerPath;
            CurrentIndex = 0;
            foreach (var imageCodec in ImageCodecInfo.GetImageEncoders())
                RecognizedExtensions.AddRange(imageCodec.FilenameExtension.ToLowerInvariant().Split(';'));
            FileNames = fileNames.Where(FileIsImage).ToArray();
            TotalFiles = FileNames.Length;
            _zipExtractor = new SevenZipExtractor(containerPath);
        }
        
        private readonly SevenZipExtractor _zipExtractor;

        public override string GetFile()
        {
            var filename = FileNames[CurrentIndex];
            var tempFile = Path.Combine(TempFolder, filename);
            var fullPath = Path.GetFullPath(tempFile);
            //TODO create folder path prior to creating file
            if (File.Exists(tempFile)) return fullPath;
            using (var stream = File.OpenWrite(tempFile))
            {
                _zipExtractor.ExtractFile(filename,stream);
            }
            return fullPath;
        }

        public override string GetFileForward()
        {
            var filename = FileNames[CurrentIndex+1];
            var tempFile = Path.Combine(TempFolder, filename);
            var fullPath = Path.GetFullPath(tempFile);
            if (File.Exists(tempFile)) return fullPath;
            using (var stream = File.OpenWrite(tempFile))
            {
                _zipExtractor.ExtractFile(filename, stream);
            }
            return fullPath;
        }
    }
}

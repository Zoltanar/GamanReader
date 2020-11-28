using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using Crc32C;
using GamanReader.ViewModel;
using JetBrains.Annotations;

// ReSharper disable VirtualMemberCallInConstructor

namespace GamanReader.Model.Database
{
	public class MangaInfo : INotifyPropertyChanged, IMangaItem
	{
		private static readonly Regex Rgx1 = new Regex(@"\[([^];]*)\]");
		private static readonly Regex Rgx2 = new Regex(@"\{([^};]*)\}");
		private static readonly Regex Rgx3 = new Regex(@"\(([^);]*)\)");

		public static bool ShowThumbs;

		#region Properties

		[NotMapped]
		public bool IsDeleted => false;

		[Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long Id { get; set; }
		public int LibraryFolderId { get; set; }
		[StringLength(1024)]
		public string SubPath { get; set; }
		public string FilePath => (Library?.Path ?? string.Empty) + SubPath;
		public string Name { get; set; }
		public DateTime LastOpened { get; set; }
		public DateTime DateAdded { get; set; }

		public int TimesBrowsed
		{
			get => _timesBrowsed;
			set
			{
				_timesBrowsed = value;
				OnPropertyChanged();
			}
		}

		public string Notes { get; set; }
		public virtual LibraryFolder Library { get; set; }
		public bool IsFolder { get; set; }
		public virtual ICollection<AutoTag> AutoTags { get; set; }
		public virtual ICollection<UserTag> UserTags { get; set; }

		// ReSharper disable once InconsistentNaming
		public string CRC32 { get; set; }
		#endregion

		public static MangaInfo Create(string filePath, bool saveToDatabase)
		{
			var pathToFind = Directory.GetParent(filePath).FullName;
			var library = !saveToDatabase
				? null
				: StaticHelpers.LocalDatabase.Libraries.SingleOrDefault(x => x.Path == pathToFind) ??
				  StaticHelpers.LocalDatabase.Libraries.Single(x => x.Id == 1);
			var item = new MangaInfo(filePath)
			{
				Library = library,
				LibraryFolderId = !saveToDatabase ? 1 : library.Id,
				SubPath = !saveToDatabase || library.Id == 1 ? filePath : filePath.Replace(library.Path, ""),
				IsFolder = File.GetAttributes(filePath).HasFlag(FileAttributes.Directory)
			};
			if (saveToDatabase) item.CalcCrc();
			return item;
		}

		/// <summary>
		/// Guesses manga information from filename.
		/// </summary>
		private MangaInfo(string filePath)
		{
			AutoTags = new HashSet<AutoTag>();
			UserTags = new HashSet<UserTag>();
			DateAdded = DateTime.Now;
			// ReSharper disable once PossibleNullReferenceException
			Name = Path.GetFileNameWithoutExtension(filePath).Trim();

			var matches = Rgx1.Matches(Name);
			var matches2 = Rgx2.Matches(Name);
			var matches3 = Rgx3.Matches(Name);
			foreach (Match match in matches)
			{
				var matchGroupValue = match.Groups[1].Value;
				var innerMatch = Rgx3.Match(matchGroupValue);
				AutoTags.Add(new AutoTag(innerMatch.Success 
					? matchGroupValue.Replace(innerMatch.Value, "").Trim()
					: matchGroupValue));
			}
			foreach (Match match in matches2) AutoTags.Add(new AutoTag(match.Groups[1].Value.Trim()));
			foreach (Match match in matches3) AutoTags.Add(new AutoTag(match.Groups[1].Value.Trim()));

		}

		/// <summary>
		/// Guesses manga information from filename.
		/// </summary>
		public static MangaInfo Create(string filePath, LibraryFolder library, bool isFolder)
		{
			var item = new MangaInfo(filePath)
			{
				LibraryFolderId = library.Id,
				Library = library,
				IsFolder = isFolder,
				SubPath = filePath.Replace(library.Path, "")
			};
			item.CalcCrc();
			if (!filePath.Equals(item.FilePath)) { }
			return item;
		}

		public MangaInfo()
		{
			Name = "Unknown";
		}

		public override string ToString() => FileCountString + Name;

		public string ToString(string format, IFormatProvider formatProvider)
		{
			return format switch
			{
				"T" => $"[{TimesBrowsed:00}]{FileCountString} {Name}",
				"D" => $"[{DateAdded:yyyyMMdd}]{FileCountString} {Name}",
				_ => ToString()
			};
		}

		private string FileCountString => FileCount.HasValue ? $"[{FileCount}]" : string.Empty;

		public bool IsFavorite => UserTags.Any(x => x.Tag.ToLower().Equals(StaticHelpers.FavoriteTagString));

		public bool IsBlacklisted => UserTags.Any(x => x.Tag.ToLower().Equals(StaticHelpers.BlacklistedTagString));

		[NotMapped]
		public bool? CantOpen { get; set; }

		[NotMapped]
		public ImageSource GetImage => IsFavorite ? StaticHelpers.GetFavoritesIcon() : null;

		[NotMapped]
		public string InfoString
		{
			get
			{
				string[] folders = Library.Path.Split('\\');
				var last2Folders = folders.Length > 2 ? folders.Skip(folders.Length - 2) : folders;
				var text = new List<string>
				{
					$"{(IsFolder ? "Folder" : Path.GetExtension(FilePath))}",
					$"{SizeMb:#0.##} MB{(FileCount > 0 ? $" ({SizeMb / FileCount:#0.##} ea)" : "")}",
					$"{string.Join("\\", last2Folders)}",
					$"{LastModified}"
				};
				return string.Join(Environment.NewLine, text);
			}
		}

		public int? FileCount { get; set; }
		private int _timesBrowsed;

		private long? _length;

		private long Length => _length ??= IsFolder ? GetImageFiles(new DirectoryInfo(FilePath), SearchOption.AllDirectories).Sum(x => x.Length) : new FileInfo(FilePath).Length;

		[NotMapped] public double SizeMb => Length / 1024d / 1024d;

		[NotMapped] private DateTime LastModified => IsFolder ? new DirectoryInfo(FilePath).LastWriteTime : new FileInfo(FilePath).LastWriteTime;

		[NotMapped] public bool ThumbnailSet { get; private set; }
		private string _thumbnail;

		public string Thumbnail => !ShowThumbs ? null : _thumbnail;

		public async Task<string> EnsureThumbExists()
		{
			if (ThumbnailSet) return _thumbnail;
			if (!(CantOpen == true || !Exists() || CRC32 == "Not Found" || CRC32 == "Deleted"))
			{
				if (string.IsNullOrWhiteSpace(CRC32)) CalcCrc();
				var thumbPath = Path.GetFullPath(Path.Combine(StaticHelpers.ThumbFolder, CRC32 + ".bmp"));
				if (File.Exists(thumbPath))
				{
					_thumbnail = thumbPath;
				}
				else
				{
					var file = await GetFirstImage();
					if (file != null)
					{
						Image image = null;
						Image thumbnailImage = null;
						try
						{
							image = Image.FromFile(file);
							var scale = Math.Min(200d / image.Width, 300d / image.Height);
							var newWidth = image.Width * scale;
							var newHeight = image.Height * scale;
							thumbnailImage = image.GetThumbnailImage((int) (newWidth), (int) (newHeight), () => true, IntPtr.Zero);
							thumbnailImage.Save(thumbPath);
						}
						catch (OutOfMemoryException)
						{
							if(Path.GetExtension(file).ToLower() == ".gif"){}
							//ignore
						}
						finally
						{
							image?.Dispose();
							thumbnailImage?.Dispose();
						}
						_thumbnail = thumbPath;
					}
				}
			}
			ThumbnailSet = true;
			OnPropertyChanged(null);
			return _thumbnail;
		}

		private async Task<string> GetFirstImage()
		{
			Container container = null;
			try
			{
				if (IsFolder)
				{
					container = new FolderContainer(this, new DirectoryInfo(FilePath).GetFiles(),MainViewModel.PageOrder.Auto);
					if (string.IsNullOrWhiteSpace(CRC32)) CalcCrc();
				}
				else
				{
					switch (Path.GetExtension(FilePath))
					{
						case ".cbz":
						case ".zip":
							container = new ZipContainer(this, null, MainViewModel.PageOrder.Auto);
							await ((ZipContainer)container).ExtractAllAsync(CancellationToken.None, 1);
							break;
						case ".rar":
							container = new RarContainer(this, null, MainViewModel.PageOrder.Auto, 1);
							break;
						default:
							throw new IndexOutOfRangeException();
					}
				}
				if (container.TotalFiles == 0) return null;
				var file = container.GetFile(0, out _);
				return file;
			}
			finally
			{
				container?.Dispose();
			}
		}


		public List<Inline> GetTbInlines()
		{
			var list = new List<Inline>();
			int inBrackets = 0;
			Hyperlink curHp = new Hyperlink();
			Run curRun = new Run();
			foreach (char c in Name)
			{
				switch (c)
				{
					case '[':
					case '(':
					case '{':
						if (inBrackets == 0 && curRun.Text.Length > 0)
						{
							list.Add(curRun);
							curRun = new Run();
						}
						else if (inBrackets > 0 && curRun.Text.Length > 0)
						{
							curHp.Inlines.Add(curRun);
							list.Add(curHp);
							curRun = new Run();
							curHp = new Hyperlink { Tag = new List<Hyperlink>() };
						}
						curRun.Text += c;
						inBrackets++;
						break;
					case ']':
					case ')':
					case '}':
						curRun.Text += c;
						if (inBrackets == 0 && curRun.Text.Length > 1)
						{
							list.Add(curRun);
							curRun = new Run();
						}
						else if (inBrackets > 0 && curRun.Text.Length > 1)
						{
							curHp.Inlines.Add(curRun);
							for (int index = list.Count - 1; index >= list.Count - (inBrackets - 1); index--)
							{
								var item = list[index];
								if (item is Hyperlink hpItem) ((List<Hyperlink>)curHp.Tag).Add(hpItem);
							}
							list.Add(curHp);
							curRun = new Run();
							curHp = new Hyperlink { Tag = new List<Hyperlink>() };
						}
						inBrackets--;
						break;
					default:
						curRun.Text += c;
						break;
				}
			}
			if (curRun.Text.Length > 0) list.Add(curRun);
			return list;
		}

		public bool Exists(LibraryFolder library = null)
		{
			var filePath = GetFilePath(library);
			var fsi = IsFolder ? (FileSystemInfo)new DirectoryInfo(filePath) : new FileInfo(filePath);
			return fsi.Exists;
		}

		public string GetFilePath(LibraryFolder library = null)
		{
			return (library ?? Library).Path + SubPath;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InfoString)));
		}

		public void CalcCrc()
		{
			if (IsFolder)
			{
				var directory = new DirectoryInfo(FilePath);
				if (!directory.Exists)
				{
					CRC32 = "Not Found";
					FileCount = 0;
				}
				else
				{
					var files = GetImageFiles(directory, SearchOption.AllDirectories);
					FileCount = files.Length;
					if (!files.Any()) CRC32 = "0";
					else
					{

						try
						{
							//var bytes = new List<byte>((long)_length.Value);
							uint crc = 0;
							foreach (var fileInfo in files.OrderBy(t => t.FullName))
							{
								var fileBytes = File.ReadAllBytes(fileInfo.FullName);
								crc = Crc32CAlgorithm.Append(crc, fileBytes);
							}
							//var contents = bytes.ToArray();//files.OrderBy(t => t.FullName).Select(t => File.ReadAllBytes(t.FullName)).ToArray();
							//var joined = bytes.ToArray(); //contents.SelectMany(c => c).ToArray();
							var crc32 = crc; //Crc32Algorithm.ComputeAndWriteToEnd(joined);
							CRC32 = crc32.ToString("X8");
						}
						catch (Exception ex)
						{
							Debug.WriteLine($"Error calculating CRC32 for item {this}.");
							Debug.WriteLine(ex);
							CRC32 = null;
						}
					}
				}
			}
			else
			{
				if (!File.Exists(FilePath))
				{
					FileCount = 0;
					CRC32 = "Not Found";
				}
				else
				{
					GetFileCountForArchive();
					var file = File.ReadAllBytes(FilePath);
					var crc32 = Crc32CAlgorithm.Compute(file);
					CRC32 = crc32.ToString("X8");
				}
			}
		}

		public void GetFileCountForArchive()
		{
			var extension = Path.GetExtension(FilePath);
			switch (extension)
			{
				case ".zip":
				case ".cbz":
					FileCount = ZipContainer.GetFileCount(FilePath);
					break;
				case ".rar":
					FileCount = RarContainer.GetFileCount(FilePath);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(extension), extension, $"Archive type not supported '{extension}'");
			}
		}

		public static FileInfo[] GetImageFiles(DirectoryInfo directoryInfo, SearchOption searchOption)
		{
			var files = directoryInfo.GetFiles("*", searchOption);
			return files.Where(fileInfo => Container.RecognizedExtensions.Contains(fileInfo.Extension)).ToArray();
		}
	}
}

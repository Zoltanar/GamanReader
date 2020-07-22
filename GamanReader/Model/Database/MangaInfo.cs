using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using Crc32C;
using JetBrains.Annotations;

// ReSharper disable VirtualMemberCallInConstructor

namespace GamanReader.Model.Database
{
	public class MangaInfo : INotifyPropertyChanged, IFormattable
	{
		#region Properties

		[Key]
		public long Id { get; set; }
		public int LibraryFolderId { get; set; }
		[StringLength(1024)]
		public string SubPath { get; set; }
		public string FilePath => Library.Path + SubPath;
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

		public static MangaInfo Create(string filePath)
		{
			var pathToFind = Directory.GetParent(filePath).FullName;
			var item = new MangaInfo(filePath);
			item.Library = StaticHelpers.LocalDatabase.Libraries.SingleOrDefault(x => x.Path == pathToFind) ??
								StaticHelpers.LocalDatabase.Libraries.Single(x => x.Id == 1);
			item.LibraryFolderId = item.Library.Id;
			item.SubPath = item.Library.Id == 1 ? filePath : filePath.Replace(item.Library.Path, "");
			item.IsFolder = File.GetAttributes(item.FilePath).HasFlag(FileAttributes.Directory);
			item.CalcCrc();
			return item;
		}
		private static readonly Regex Rgx1 = new Regex(@"\[([^];]*)\]");
		private static readonly Regex Rgx2 = new Regex(@"\{([^};]*)\}");
		private static readonly Regex Rgx3 = new Regex(@"\(([^);]*)\)");

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
				var innerMatch = Rgx3.Match(match.Groups[1].Value);
				if (innerMatch.Success)
				{
					AutoTags.Add(new AutoTag(match.Groups[1].Value.Replace(innerMatch.Value, "").Trim()));
				}
				else AutoTags.Add(new AutoTag(match.Groups[1].Value));
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
			if (!filePath.Equals(item.FilePath)) { }
			return item;
		}

		public MangaInfo()
		{
			Name = "Unknown";
		}

		public override string ToString() => Name;

		public string ToString(string format, IFormatProvider formatProvider)
		{
			return format switch
			{
				"T" => $"[{TimesBrowsed:00}] {Name}",
				"D" => $"[{DateAdded:yyyyMMdd}] {Name}",
				_ => ToString()
			};
		}
		
		public bool IsFavorite => UserTags.Any(x => x.Tag.ToLower().Equals(StaticHelpers.FavoriteTagString));

		public bool IsBlacklisted => UserTags.Any(x => x.Tag.ToLower().Equals(StaticHelpers.BlacklistedTagString));
		
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

		public int FileCount = 0;
		private int _timesBrowsed;

		private long? _length;

		[NotMapped]
		public double SizeMb
		{
			get
			{
				if (!_length.HasValue)
				{
					_length = IsFolder ? new DirectoryInfo(FilePath).GetFiles().Sum(x => x.Length) : new FileInfo(FilePath).Length;
				}
				return _length.Value / 1024d / 1024d;
			}
		}

		[NotMapped]
		private DateTime LastModified => IsFolder ? new DirectoryInfo(FilePath).LastWriteTime : new FileInfo(FilePath).LastWriteTime;


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
							curHp = new Hyperlink {Tag = new List<Hyperlink>()};
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
							for (int index = list.Count - 1; index >= list.Count - (inBrackets-1); index--)
							{
								var item = list[index];
								if(item is Hyperlink hpItem) (curHp.Tag as List<Hyperlink>).Add(hpItem);
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

		public bool Exists()
		{
			var fsi = IsFolder ? (FileSystemInfo)new DirectoryInfo(FilePath) : new FileInfo(FilePath);
			return fsi.Exists;
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
			//if (!IsFolder && !string.IsNullOrWhiteSpace(CRC32)) return;
			if (CRC32 == "Deleted") return;
			if (IsFolder)
			{
				var directory = new DirectoryInfo(FilePath);
				if(!directory.Exists) CRC32 = "Not Found";
				else
				{
					var files = directory.GetFiles();
					if (!files.Any()) CRC32 = "0";
					else
					{
						var sizemb = SizeMb;
						if (_length > int.MaxValue)
						{
							//CRC32 = null;
							//return;
						}
						try
						{
							//var bytes = new List<byte>((long)_length.Value);
							uint crc = 0;
							foreach (var fileInfo in files.OrderBy(t => t.FullName))
							{
								var fileBytes = File.ReadAllBytes(fileInfo.FullName);
								crc = Crc32CAlgorithm.Append(crc,fileBytes);
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
				if(!File.Exists(FilePath)) CRC32 = "Not Found";
				else
				{
					var file = File.ReadAllBytes(FilePath);
					var crc32 = Crc32CAlgorithm.Compute(file);
					CRC32 = crc32.ToString("X8");
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
// ReSharper disable VirtualMemberCallInConstructor

namespace GamanReader.Model.Database
{
	public class MangaInfo
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
		public int TimesBrowsed { get; set; }
		public string Notes { get; set; }
	public virtual LibraryFolder Library { get; set; }
		public bool IsFolder { get; set; }
		public virtual ICollection<AutoTag> AutoTags { get; set; }
		public virtual ICollection<UserTag> UserTags { get; set; }

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
			var rgx1 = new Regex(@"\[([^];]*)\]");
			var rgx2 = new Regex(@"\{([^};]*)\}");
			var rgx3 = new Regex(@"\(([^);]*)\)");
			var matches = rgx1.Matches(Name);
			var matches2 = rgx2.Matches(Name);
			var matches3 = rgx3.Matches(Name);
			foreach (Match match in matches)
			{
				var innerMatch = rgx3.Match(match.Groups[1].Value);
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


		public bool IsFavorite => UserTags.Any(x => x.Tag.ToLower().Equals("favorite"));

		public bool IsBlacklisted => UserTags.Any(x => x.Tag.ToLower().Equals("blacklisted"));

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
					$"{LastModified}",
					$"Times Browsed: {TimesBrowsed}"
				};
				return string.Join(Environment.NewLine, text);
			}
		}

		public int FileCount = 0;

		[NotMapped]
		private double SizeMb => (double)(IsFolder ? new DirectoryInfo(FilePath).GetFiles().Sum(x => x.Length) : new FileInfo(FilePath).Length) / 1024 / 1024;
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
							curHp = new Hyperlink();
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
							list.Add(curHp);
							curRun = new Run();
							curHp = new Hyperlink();
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
	}
}

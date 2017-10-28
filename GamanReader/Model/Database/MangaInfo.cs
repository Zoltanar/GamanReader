using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
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
			// ReSharper disable once PossibleNullReferenceException
			Name = Path.GetFileNameWithoutExtension(filePath).Trim();
			var rgx1 = new Regex(@"\[([^];]*)\]");
			var rgx2 = new Regex(@"\{([^];]*)\}");
			var matches = rgx1.Matches(Name);
			var matches2 = rgx2.Matches(Name);
			var rgx3 = new Regex(@"\(([^];]*)\)");
			foreach (Match match in matches)
			{
				var innerMatch = rgx3.Match(match.Groups[1].Value);
				if (innerMatch.Success)
				{
					AutoTags.Add(new AutoTag(match.Groups[1].Value.Replace(innerMatch.Value, "")));
					AutoTags.Add(new AutoTag(innerMatch.Groups[1].Value));
				}
				else AutoTags.Add(new AutoTag(match.Groups[1].Value));
			}
			foreach (Match match in matches2) AutoTags.Add(new AutoTag(match.Groups[1].Value));
			
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


		public bool IsFavorite() => UserTags.Any(item => item.Tag == "favorite");

		[NotMapped]
		public ImageSource GetImage => IsFavorite() ? StaticHelpers.GetFavoritesIcon() : null;


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
			if (curRun.Text.Length > 0)
			{
				list.Add(curRun);
			}
			foreach (var inline in list)
			{
				if(inline is Hyperlink link) Debug.WriteLine("Link: " +((Run) link.Inlines.FirstInline).Text);
				else if(inline is Run run) Debug.WriteLine("Plain: " + run.Text);
			}
			return list;
		}
	}
}

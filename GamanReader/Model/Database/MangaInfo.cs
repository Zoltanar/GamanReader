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
using System.Windows.Media;
using JetBrains.Annotations;

namespace GamanReader.Model.Database
{
	public class MangaInfo : INotifyPropertyChanged
	{
		#region Properties
		private string _event;
		private string _group;
		private string _artist;
		private string _title;
		private string _parody;
		private string _subber;

		[Key]
		public long Id { get; set; }

		public int LibraryFolderId { get; set; }

		[StringLength(1024)]
		public string SubPath { get; set; }

		public string FilePath => Library.Path + SubPath;
		public string Event
		{
			get => _event;
			set
			{
				_event = value?.Trim();
				OnPropertyChanged();
			}
		}
		public string Group
		{
			get => _group;
			set
			{
				_group = value?.Trim();
				OnPropertyChanged();
			}
		}
		public string Artist
		{
			get => _artist;
			set
			{
				_artist = value?.Trim();
				OnPropertyChanged();
			}
		}
		public string Title
		{
			get => _title;
			set
			{
				_title = value?.Trim();
				OnPropertyChanged();
			}
		}
		public string Parody
		{
			get => _parody;
			set
			{
				_parody = value?.Trim();
				OnPropertyChanged();
			}
		}
		public string Subber
		{
			get => _subber;
			set
			{
				_subber = value?.Trim();
				OnPropertyChanged();
			}
		}

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
			if (!filePath.Equals(item.FilePath)) { }
			return item;
		}

		/// <summary>
		/// Guesses manga information from filename.
		/// </summary>
		private MangaInfo(string filePath)
		{
			if (filePath.ToLower().Contains("(Sono")) { }
			AutoTags = new HashSet<AutoTag>();
			UserTags = new HashSet<UserTag>();
			var filename = Path.GetFileNameWithoutExtension(filePath).Trim();
			//modifications for easier parsing
			if (filename.EndsWith("-1280x"))
			{
				filename = filename.RemoveFromEnd("-1280x".Length);
				AutoTags.Add(new AutoTag(this, "1280x"));
			}
			if (filename.Contains("=LWB=")) filename = filename.Replace("=LWB=", "[LWB]");
			if (filename.ToLower().EndsWith("updated"))
			{
				filename = filename.RemoveFromEnd("updated".Length);
				AutoTags.Add(new AutoTag(this, "Updated"));
			}
			if (filename.ToLower().EndsWith("fixed"))
			{
				filename = filename.RemoveFromEnd("fixed".Length);
				AutoTags.Add(new AutoTag(this, "Fixed"));
			}
			if (filename.ToLower().EndsWith("v1") || filename.ToLower().EndsWith("v2") || filename.ToLower().EndsWith("v3"))
			{
				filename = filename.RemoveFromEnd("v0".Length);
				AutoTags.Add(new AutoTag(this, filename.BetweenIndexes(filename.Length-3,filename.Length-1)));
			}
			if (filename.ToLower().EndsWith("eng"))
			{
				filename = filename.RemoveFromEnd("eng".Length);
				AutoTags.Add(new AutoTag(this, "English"));
			}
			filename = filename.Trim();
			Debug.Assert(filename != null, nameof(filename) + " != null");
			var startOfTitle = ProcessPreTitleParts(filename);
			//
			int lastCharacterOutsideOfBrackets = 0;
			{
				int index = startOfTitle;
				int inBracket = 0;
				bool inEqualBracket = false;
				while (index < filename.Length)
				{
					switch (filename[index])
					{
						case ' ':
							break;
						case '[':
						case '{':
						case '(':
							inBracket++;
							break;
						case ']':
						case '}':
						case ')':
							inBracket--;
							break;
						case '=':
							if (!inEqualBracket) inBracket++;
							else inBracket--;
							inEqualBracket = !inEqualBracket;
							break;
						default:
							if (inBracket == 0) lastCharacterOutsideOfBrackets = index;
							break;
					}
					index++;
				}
			}

			//
			var startOfPost = lastCharacterOutsideOfBrackets == filename.Length-1 ? -1 : lastCharacterOutsideOfBrackets + 1;
			if (startOfTitle == filename.Length) Title = filename;
			else if (startOfPost > -1)
			{
				if (filename[startOfPost] == ' ') startOfPost++;
				Title = filename.BetweenIndexes(startOfTitle, startOfPost - 1);
				ProcessPostTitleParts(filename.Substring(startOfPost));
			}
			else Title = filename.Substring(startOfTitle);
		}

		/// <summary>
		/// Returns index of start of title.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns>Index of start of title.</returns>
		private int ProcessPreTitleParts(string filename)
		{
			int cIndex = 0;
			List<string> preParts = new List<string>();
			while (cIndex < filename.Length)
			{
				char c = filename[cIndex];
				if (c == '(')
				{
					for (int i = cIndex + 1; i < filename.Length; i++)
					{
						if (filename[i] != ')') continue;
						preParts.Add(filename.BetweenIndexes(cIndex + 1, i - 1));
						cIndex = i;
						break;
					}
				}
				else if (c == '[')
				{
					for (int i = cIndex + 1; i < filename.Length; i++)
					{
						if (filename[i] != ']') continue;
						preParts.Add(filename.BetweenIndexes(cIndex + 1, i - 1));
						cIndex = i;
						break;
					}
				}
				else if (c != ' ')
				{
					break;
				}
				cIndex++;
			}
			if (preParts.Count == 0) return cIndex;
			var rgx1 = new Regex(@"\((.*?)\)");
			var match = rgx1.Match(preParts.Last());
			if (string.IsNullOrWhiteSpace(match.Value)) Artist = preParts.Last();
			else
			{
				Group = preParts.Last().BetweenIndexes(0, preParts.Last().IndexOf("(", StringComparison.Ordinal) - 1);
				Artist = match.Groups[1].Value;
			}
			//
			for (int i = preParts.Count - 1; i >= 0; i--)
			{
				var item = preParts[i];
				switch (item.ToLower())
				{
					case "同人誌":
						AutoTags.Add(new AutoTag(this, "Doujinshi"));
						preParts.Remove(item);
						continue;
					default:
						var rgx = new Regex(@"^[0-9]+$");
						if (rgx.IsMatch(item)) preParts.Remove(item);
						continue;
				}
			}
			if (preParts.Count == 2) Event = preParts.First();
			if (preParts.Count > 2) { }
			return cIndex;
		}

		private void ProcessPostTitleParts(string filename)
		{
			int cIndex = 0;
			List<(string, bool)> postParts = new List<(string, bool)>();
			while (true)
			{
				char c = filename[cIndex];
				if (c == '(')
				{
					for (int i = cIndex + 1; i < filename.Length; i++)
					{
						if (filename[i] != ')') continue;
						postParts.Add((filename.BetweenIndexes(cIndex + 1, i - 1), true));
						cIndex = i;
						break;
					}
				}
				else if (c == '[')
				{
					for (int i = cIndex + 1; i < filename.Length; i++)
					{
						if (filename[i] != ']') continue;
						postParts.Add((filename.BetweenIndexes(cIndex + 1, i - 1), false));
						cIndex = i;
						break;
					}
				}
				else if (c == '=')
				{
					for (int i = cIndex + 1; i < filename.Length; i++)
					{
						if (filename[i] != '=') continue;
						postParts.Add((filename.BetweenIndexes(cIndex + 1, i - 1), false));
						cIndex = i;
						break;
					}
				}
				cIndex++;
				if (cIndex > filename.Length - 1) break;
			}
			if (postParts.Count == 0) return;
			for (int i = postParts.Count - 1; i >= 0; i--)
			{
				var item = postParts[i];
				switch (item.Item1.ToLower())
				{
					case "1280x":
						AutoTags.Add(new AutoTag(this, "1280x"));
						postParts.Remove(item);
						continue;
					case "en":
					case "eng":
					case "english":
						AutoTags.Add(new AutoTag(this, "English"));
						postParts.Remove(item);
						continue;
					case "chinese":
						AutoTags.Add(new AutoTag(this, "Chinese"));
						postParts.Remove(item);
						continue;
					case "complete":
					case "completed":
						AutoTags.Add(new AutoTag(this, "Complete"));
						postParts.Remove(item);
						continue;
					case "incompleted":
					case "incomplete":
						AutoTags.Add(new AutoTag(this, "Incomplete"));
						postParts.Remove(item);
						continue;
					case "colorised":
					case "colorized":
						AutoTags.Add(new AutoTag(this, "Colorized"));
						postParts.Remove(item);
						continue;
					case "digital":
						AutoTags.Add(new AutoTag(this, "Digital"));
						postParts.Remove(item);
						continue;
					case "uncensored":
					case "decensored":
						AutoTags.Add(new AutoTag(this, "Decensored"));
						postParts.Remove(item);
						continue;
					default:
						//these are ignored
						var dateRgx = new Regex(@"\d\d\d\d-\d\d-\d\d");
						if (item.Item1.ToLower().StartsWith("part ") ||
								item.Item1.ToLower().StartsWith("chapter ") ||
								item.Item1.ToLower().StartsWith("ch. ") ||
								(item.Item1.ToLower().StartsWith("v") && item.Item1.Length == 2) ||
						    dateRgx.IsMatch(item.Item1))
						{
							postParts.Remove(item);
							continue;
						}
						if (item.Item1.ToLower().Contains("comic"))
						{
							Event = item.Item1;
							postParts.Remove(item);
							continue;
						}
						break;
				}
				if (Parody == null && item.Item2)
				{
					Parody = item.Item1;
					postParts.Remove(item);
					continue;
				}
			}
			if (postParts.Count == 1) Subber = postParts[0].Item1;
			else if (postParts.Count > 1)
			{
				Subber = postParts.Last().Item1;
				postParts.RemoveAt(postParts.Count - 1);
				postParts.ForEach(x => AutoTags.Add(new AutoTag(this, x.Item1)));
			}
			else return;
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
			Title = "Unknown";
		}

		private void GetSubberAndFlags(string filename)
		{
			var rgx2 = new Regex(@"\[(.*?)\]");
			var matches2 = rgx2.Matches(filename);
			var rgx3 = new Regex(@"\{(.*?)\}");
			var matches3 = rgx3.Matches(filename);
			foreach (Match match in matches2)
			{
				switch (match.Groups[1].Value.ToLower())
				{
					case "eng":
					case "english":
						AutoTags.Add(new AutoTag(this, "English"));
						break;
					default:
						AutoTags.Add(new AutoTag(this, match.Groups[1].Value));
						break;
				}
			}
			if (matches3.Count > 0) Subber = matches3[0].Groups[1].Value;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public override string ToString() => Title;


		public bool IsFavorite() => UserTags.Any(item => item.Tag == "favorite");

		[NotMapped]
		public ImageSource GetImage => IsFavorite() ? StaticHelpers.GetFavoritesIcon() : null;
	}
}

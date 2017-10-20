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

		[Key, Column(Order = 0)]
		public int LibraryFolderId { get; set; }
		[Key, Column(Order = 1), StringLength(1024)]
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

		public bool Incomplete { get; set; }
		public bool Decensored { get; set; }
		public bool English { get; set; }
		public bool Digital { get; set; }

		public virtual LibraryFolder Library { get; set; }
		public bool IsFolder { get; set; }
		public virtual ICollection<string> Tags { get; set; }

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
			Tags = new HashSet<string>();
			var filename = Path.GetFileNameWithoutExtension(filePath);
			Debug.Assert(filename != null, nameof(filename) + " != null");
			var firstClosingBracket = filename.IndexOf(")", StringComparison.Ordinal);
			if (filename.StartsWith("(") && firstClosingBracket > -1)
			{
				Event = filename.BetweenIndexes(1, firstClosingBracket - 1);
				filename = filename.Substring(firstClosingBracket + 1);
				filename = filename.Trim();
			}
			var firstClosingSquareBracket = filename.IndexOf("]", StringComparison.Ordinal);
			if (filename.StartsWith("[") && firstClosingSquareBracket > -1)
			{
				var groupArtist = filename.BetweenIndexes(1, firstClosingSquareBracket - 1);
				var rgx1 = new Regex(@"\((.*?)\)");
				var match = rgx1.Match(groupArtist);
				if (string.IsNullOrWhiteSpace(match.Value)) Artist = groupArtist;
				else
				{
					Group = groupArtist.BetweenIndexes(0, groupArtist.IndexOf("(", StringComparison.Ordinal) - 1);
					Artist = match.Groups[1].Value;
				}
				filename = filename.Substring(firstClosingSquareBracket + 1);
				filename = filename.Trim();
			}
			var postTitleBracket = filename.IndexOfAny(new[] { '(', '[' });
			if (postTitleBracket > -1)
			{
				Title = filename.BetweenIndexes(0, postTitleBracket - 1);
				var postTitleClosingBracket = filename.IndexOf(")", StringComparison.Ordinal);
				if (filename[postTitleBracket] == '(' && postTitleClosingBracket > postTitleBracket)
				{
					Parody = filename.BetweenIndexes(postTitleBracket + 1, postTitleClosingBracket - 1);
				}
				GetSubberAndFlags(filename);
			}
			else Title = filename;
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
				switch (match.Groups[1].Value)
				{
					case "ENG":
					case "English":
						Tags.Add("English");
						English = true;
						break;
					default:
						Tags.Add(match.Groups[1].Value);
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
	}
}

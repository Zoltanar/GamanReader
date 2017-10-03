﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace GamanReader.Model
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

		public string Event
		{
			get => _event;
			set
			{
				_event = value.Trim();
				OnPropertyChanged();
			}
		}
		public string Group
		{
			get => _group;
			set
			{
				_group = value.Trim();
				OnPropertyChanged();
			}
		}
		public string Artist
		{
			get => _artist;
			set
			{
				_artist = value.Trim();
				OnPropertyChanged();
			}
		}
		public string Title
		{
			get => _title;
			set
			{
				_title = value.Trim();
				OnPropertyChanged();
			}
		}
		public string Parody
		{
			get => _parody;
			set
			{
				_parody = value.Trim();
				OnPropertyChanged();
			}
		}
		public string Subber
		{
			get => _subber;
			set
			{
				_subber = value.Trim();
				OnPropertyChanged();
			}
		}

		public bool Incomplete { get; set; }
		public bool Decensored { get; set; }
		public bool English { get; set; }
		public bool Digital { get; set; }
		#endregion

		/// <summary>
		/// Guesses manga information from filename.
		/// </summary>
		public MangaInfo(string filename)
		{
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
				if (filename[postTitleBracket] == '(')
				{
					Parody = filename.BetweenIndexes(postTitleBracket + 1, filename.IndexOf(")", StringComparison.Ordinal) - 1);
				}
				GetSubberAndFlags(filename);
			}
			else Title = filename;
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
					case "Digital":
						Digital = true;
						break;
					case "English":
						English = true;
						break;
					case "Decensored":
						Decensored = true;
						break;
					case "Incomplete":
						Incomplete = true;
						break;
					default:
						Subber = match.Groups[1].Value;
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
	}
}

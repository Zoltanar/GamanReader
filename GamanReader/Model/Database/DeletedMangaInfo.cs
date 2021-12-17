using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using JetBrains.Annotations;

namespace GamanReader.Model.Database
{
	public sealed class DeletedMangaInfo : IMangaItem, INotifyPropertyChanged
	{
		#region Properties
		[Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long Id { get; set; }
		public string Name { get; set; }
		public string NoTagName => StaticHelpers.GetNoTagName(Name);
		[NotMapped] public int TimesBrowsed => 0;
		[NotMapped] public bool IsDeleted => true;
		[NotMapped] public bool IsFavorite => false;
		[NotMapped] public bool IsBlacklisted => false;
		[NotMapped] public bool? CantOpen => true;
		public List<Inline> GetDescriptiveTitle() => StaticHelpers.GetTbInlines(Name);
		public DateTime DateAdded { get; set; }

#pragma warning disable 1998
		public string Thumbnail => null;
		public string EnsureThumbExists() => null;
		[NotMapped] public bool ThumbnailSet { get; } = true;
		public bool ShowThumbnail => MangaInfo.ShowThumbnailForAll;
#pragma warning restore 1998

		public DateTime DateDeleted { get; set; }
		public bool IsFolder { get; set; }
		// ReSharper disable once InconsistentNaming
		public string CRC32 { get; set; }
		public int? FileCount { get; set; }
		public double SizeMb { get; set; }
		public string FilePath { get; set; }
		public string FileCountString => FileCount.HasValue ? $"[{FileCount}]" : string.Empty;

		#endregion

		public DeletedMangaInfo()
		{

		}

		public DeletedMangaInfo(MangaInfo item)
		{
			Id = item.Id;
			Name = item.Name;
			DateAdded = item.DateAdded;
			DateDeleted = DateTime.Now;
			IsFolder = item.IsFolder;
			CRC32 = item.CRC32;
			SizeMb = item.SizeMb;
			FilePath = item.FilePath;
			FileCount = item.FileCount;
		}

		public override string ToString() => FileCountString + Name;

		public string ToString(string format, IFormatProvider formatProvider)
		{
			return format switch
			{
				"A" => $"[{DateAdded:yyyyMMdd}]{FileCountString} {Name}",
				"T" => $"[{DateDeleted:yyyyMMdd}]{FileCountString} {Name}",
				_ => ToString()
			};
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
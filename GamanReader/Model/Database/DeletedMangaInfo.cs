using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace GamanReader.Model.Database
{
	public sealed class DeletedMangaInfo : IFormattable
	{
		#region Properties
		[Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
		public long Id { get; set; }
		public string Name { get; set; }
		public DateTime DateAdded { get; set; }
		public DateTime DateDeleted { get; set; }
		public bool IsFolder { get; set; }
		// ReSharper disable once InconsistentNaming
		public string CRC32 { get; set; }
		public int? FileCount { get; set; }
		public double SizeMb { get; set; }
		public string FilePath { get; set; }
		private string FileCountString => FileCount.HasValue ? $"[{FileCount}]" : string.Empty;

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
			SizeMb = item.Exists() ? item.SizeMb : 0d;
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

		[NotMapped]
		public string InfoString
		{
			get
			{
				var text = new List<string>
				{
					$"{(IsFolder ? "Folder" : Path.GetExtension(FilePath))}",
					$"{SizeMb:#0.##} MB{(FileCount > 0 ? $" ({SizeMb / FileCount:#0.##} ea)" : "")}",
					$"{FilePath}"
				};
				return string.Join(Environment.NewLine, text);
			}
		}
	}
}
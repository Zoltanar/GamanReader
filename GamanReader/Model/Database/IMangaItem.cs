using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Documents;

namespace GamanReader.Model.Database
{
	public interface IMangaItem : IFormattable, INotifyPropertyChanged
	{
		long Id { get; }
		string Name { get; }
		string NoTagName { get; }
		int TimesBrowsed { get; }
		string FileCountString { get; }
		bool IsDeleted { get; }
		bool IsFavorite { get; }
		bool IsBlacklisted { get; }
		bool? CantOpen { get;}
		DateTime DateAdded { get; }
		string Thumbnail { get; }
		string EnsureThumbExists();
		bool ThumbnailSet { get; }
		bool ShowThumbnail { get; }
		List<Inline> GetDescriptiveTitle();
		void OnPropertyChanged(string propertyName);
	}
}

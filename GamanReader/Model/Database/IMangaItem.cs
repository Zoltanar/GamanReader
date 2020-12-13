using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace GamanReader.Model.Database
{
	public interface IMangaItem : IFormattable, INotifyPropertyChanged
	{
		long Id { get; }
		string Name { get; }
		int TimesBrowsed { get; }
		bool IsDeleted { get; }
		bool IsFavorite { get; }
		bool IsBlacklisted { get; }
		bool? CantOpen { get;}
		DateTime DateAdded { get; }
		string Thumbnail { get; }
		Task<string> EnsureThumbExists();
		bool ThumbnailSet { get; }
		bool ShowThumbnail { get; }
		void OnPropertyChanged(string propertyName);
	}
}

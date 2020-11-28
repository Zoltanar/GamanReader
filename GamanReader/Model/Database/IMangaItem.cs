using System;
using System.Threading.Tasks;

namespace GamanReader.Model.Database
{
	public interface IMangaItem : IFormattable
	{
		long Id { get; }
		string Name { get; }
		int TimesBrowsed { get; }
		bool IsDeleted { get; }
		bool IsFavorite { get; }
		bool IsBlacklisted { get; }
		bool? CantOpen { get;}
		DateTime DateAdded { get; }
		Task<string> EnsureThumbExists();
		bool ThumbnailSet { get; }
	}
}

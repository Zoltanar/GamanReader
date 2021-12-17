using System.ComponentModel.DataAnnotations.Schema;

namespace GamanReader.Model.Database
{
	public class PageTag
	{
		public int Id { get; set; }

		public long ItemId { get; set; }

		[NotMapped]
		public int Page { get; set; }

		public string FileName { get; set; }

		public virtual MangaInfo Item { get; set; }

	}
}

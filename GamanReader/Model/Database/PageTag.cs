namespace GamanReader.Model.Database
{
	public class PageTag
	{
		public int Id { get; set; }

		public int ItemId { get; set; }

		public int Index { get; set; }

		public string FileName { get; set; }

		public virtual MangaInfo Item { get; set; }

	}
}

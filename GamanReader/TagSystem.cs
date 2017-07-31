using SQLite.CodeFirst;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GamanReader
{
	class TagDatabase : DbContext
	{
		public TagDatabase() : base("TagDatabase") { }
		public DbSet<TaggedItem> TaggedItems { get; set; }
		public DbSet<IndividualTag> Tags { get; set; }
		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<TagDatabase>(modelBuilder);
			Database.SetInitializer(sqliteConnectionInitializer);
		}
	}
	public class TaggedItem
	{
		public int Id { get; set; }
		public string Path { get; set; }
		public byte[] MD5Hash { get; set; }
		public bool IsFolder { get; set; }
		public virtual ICollection<IndividualTag> Tags { get; set; }
		
		public bool IsFavorite() => Tags.Any(item => item.Tag == "favorite");

		[NotMapped]
		public ImageSource GetImage => IsFavorite() ? StaticHelpers.GetFavoritesIcon() : null;

		[NotMapped]
		public string GetName => System.IO.Path.GetFileName(Path);
	}

	public class IndividualTag
	{
		public int Id { get; set; }
		public int TaggedItemId { get; set; }
		public string Tag { get; set; }

		public virtual TaggedItem TaggedItem { get; set; }
	}
}

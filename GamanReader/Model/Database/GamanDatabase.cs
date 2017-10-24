using System.Data.Entity;
using SQLite.CodeFirst;

namespace GamanReader.Model.Database
{
	public class GamanDatabase : DbContext
	{
		public GamanDatabase() : base("TagDatabase") { }
		public DbSet<LibraryFolder> Libraries { get; set; }
		public DbSet<MangaInfo> Items { get; set; }
		public DbSet<AutoTag> AutoTags { get; set; }
		public DbSet<UserTag> UserTags { get; set; }
		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<GamanDatabase>(modelBuilder);
			System.Data.Entity.Database.SetInitializer(sqliteConnectionInitializer);
		}
	}

	public class IndividualTag
	{
		public int Id { get; set; }
		public long ItemId { get; set; }
		public string Tag { get; set; }

		public virtual MangaInfo Item { get; set; }

		public IndividualTag(MangaInfo item, string tag)
		{
			ItemId = item.Id;
			Tag = tag;
		}

		public IndividualTag() { }
	}

	public class AutoTag : IndividualTag
	{
		public AutoTag(MangaInfo item, string tag) : base(item,tag){}
		public AutoTag() { }
		
	}
	public class UserTag : IndividualTag
	{
		public UserTag(MangaInfo item, string tag) : base(item,tag){ }
		public UserTag() { }
	}
}

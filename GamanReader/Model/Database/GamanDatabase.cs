using System;
using System.Data.Entity;
using SQLite.CodeFirst;
using System.Linq;

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

		public IQueryable<MangaInfo> GetRecentItems(int itemCount)
			=> Items.Where(x=>x.LastOpened != DateTime.MinValue).OrderByDescending(x => x.LastOpened).Take(itemCount);
	}
	

	public class IndividualTag
	{
		public int Id { get; set; }
		public long ItemId { get; set; }
		public string Tag { get; set; }

		public virtual MangaInfo Item { get; set; }

		protected IndividualTag(string tag)
		{
			Tag = tag;
		}

		public IndividualTag() { }
	}

	public class AutoTag : IndividualTag
	{
		public AutoTag(string tag) : base(tag){}
		public AutoTag() { }
		
	}
	public class UserTag : IndividualTag
	{
		public UserTag(long itemId, string tag) : base(tag)
		{
			ItemId = itemId;
		}
		public UserTag() { }
	}
}

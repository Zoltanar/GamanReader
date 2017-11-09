using System;
using System.Data.Entity;
using SQLite.CodeFirst;
using System.Linq;
using GamanReader.View;

// ReSharper disable VirtualMemberCallInConstructor

namespace GamanReader.Model.Database
{
	public class GamanDatabase : DbContext
	{
		public TagTreePanel TagPanel;
		public GamanDatabase() : base("TagDatabase") { }
		public DbSet<LibraryFolder> Libraries { get; set; }
		public DbSet<MangaInfo> Items { get; set; }
		public DbSet<Alias> Aliases { get; set; }
		public DbSet<AliasTag> AliasTags { get; set; }
		public DbSet<AutoTag> AutoTags { get; set; }
		public DbSet<UserTag> UserTags { get; set; }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<GamanDatabase>(modelBuilder);
			System.Data.Entity.Database.SetInitializer(sqliteConnectionInitializer);
		}

		public IQueryable<MangaInfo> GetLastOpened(int itemCount)
			=> Items.Where(x => x.LastOpened != DateTime.MinValue).OrderByDescending(x => x.LastOpened).Take(itemCount);

		public IQueryable<MangaInfo> GetLastAdded(int itemCount) => Items.OrderByDescending(x => x.DateAdded).Take(itemCount);


		public Alias GetOrCreateAlias(string aliasName)
		{
			var item = GetByName(aliasName);
			if (item != null) return item;
			StaticHelpers.LocalDatabase.Aliases.Add(new Alias(aliasName));
			StaticHelpers.LocalDatabase.SaveChanges();
			item = GetByName(aliasName);
			return item;

			Alias GetByName(string name)
			{
				return StaticHelpers.LocalDatabase.Aliases.FirstOrDefault(x => x.Name.ToLower().Equals(name.ToLower()));
			}
		}

		public MangaInfo GetOrCreateMangaInfo(string containerPath, RecentItemList<MangaInfo> lastAddedCollection)
		{
			var item = GetByPath(containerPath);
			if (item != null) return item;
			var preSavedItem = MangaInfo.Create(containerPath);
			Items.Add(preSavedItem);
			SaveChanges();
			item = GetByPath(containerPath);
			lastAddedCollection?.Add(item);
			return item;

			MangaInfo GetByPath(string path)
			{
				var items = Items.Where(x => path.EndsWith(x.SubPath)).ToArray();
				return items.FirstOrDefault(x => x.FilePath == path);
			}
		}
		

		public void AddTag(MangaInfo item, string tag)
		{
			if (item.UserTags.Any(x => x.Tag.ToLower().Equals(tag.ToLower()))) return;
			item.UserTags.Add(new UserTag(item.Id, tag));
			SaveChanges();
			TagPanel.AddTag(item,tag);
		}

		public void RemoveTag(MangaInfo item, string tag)
		{
			var tagItem = item.UserTags.FirstOrDefault(x => x.Tag.ToLower().Equals(tag.ToLower()));
			if (tagItem == null) return;
			item.UserTags.Remove(tagItem);
			UserTags.Remove(tagItem);
			SaveChanges();
			TagPanel.RemoveTag(item, tag);
		}
	}
	
}

using System;
using System.Collections.Generic;
using System.Data.Entity;
using SQLite.CodeFirst;
using System.Linq;
// ReSharper disable VirtualMemberCallInConstructor

namespace GamanReader.Model.Database
{
	public class GamanDatabase : DbContext
	{
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
	}

	public class AliasTag
	{
		public int Id { get; set; }
		public int AliasId { get; set; }
		public string Tag { get; set; }
		public virtual Alias Alias { get; set;}
	}

	public class Alias
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public virtual List<AliasTag> AliasTags { get; set; }
		public IEnumerable<string> Tags => AliasTags.Select(x => x.Tag);

		public Alias() { }

		public Alias(string name)
		{
			Name = name;
			AliasTags = new List<AliasTag>();
		}

		public void AddTags(IEnumerable<IndividualTag> tags)
		{
			foreach (var iTag in tags)
			{
				if (Tags.Contains(iTag.Tag.ToLower())) continue;
				AliasTags.Add(new AliasTag{AliasId = Id, Tag = iTag.Tag.ToLower()});
			}
		}

		public override string ToString() => Name;
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

		public override string ToString() => Tag;
	}

	public class AutoTag : IndividualTag
	{
		public AutoTag(string tag) : base(tag) { }
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

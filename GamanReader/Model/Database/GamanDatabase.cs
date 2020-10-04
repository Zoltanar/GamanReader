using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Diagnostics;
using SQLite.CodeFirst;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using GamanReader.View;

// ReSharper disable VirtualMemberCallInConstructor

namespace GamanReader.Model.Database
{
	public class GamanDatabase : DbContext
	{
		public TagTreePanel TagPanel;

		public GamanDatabase() : base("TagDatabase")
		{
		}

		public DbSet<LibraryFolder> Libraries { get; set; }
		public DbSet<MangaInfo> Items { get; set; }
		public DbSet<DeletedMangaInfo> DeletedItems { get; set; }
		public DbSet<Alias> Aliases { get; set; }
		public DbSet<AliasTag> AliasTags { get; set; }
		public DbSet<AutoTag> AutoTags { get; set; }
		public DbSet<UserTag> UserTags { get; set; }

		static GamanDatabase()
		{
			DbInterception.Add(new SqliteInterceptCharIndex());
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<GamanDatabase>(modelBuilder);
			System.Data.Entity.Database.SetInitializer(sqliteConnectionInitializer);
		}

		public IEnumerable<MangaInfo> GetLastOpened(int itemCount)
			=> Items.Where(x => x.LastOpened != DateTime.MinValue).OrderByDescending(x => x.LastOpened).Take(itemCount);

		public IEnumerable<MangaInfo> GetLastAdded(int itemCount) =>
			Items.OrderByDescending(x => x.DateAdded).Take(itemCount);

		public IEnumerable<MangaInfo> GetMostBrowsed(int itemCount) =>
			Items.AsEnumerable().Where(i => !i.IsFavorite && !i.IsBlacklisted && i.TimesBrowsed > 0).OrderByDescending(x => x.TimesBrowsed).Take(itemCount);

		public IEnumerable<MangaInfo> GetNotBrowsed(int itemCount) =>
			Items.AsEnumerable().Where(i => /*!i.IsFavorite &&*/ !i.IsBlacklisted && i.TimesBrowsed == 0 && i.Exists()).OrderByDescending(x => x.DateAdded).Take(itemCount);

		public void DeleteMangaInfo(MangaInfo item, bool addToDeletedItems)
		{
			if (addToDeletedItems)
			{
				var deleted = new DeletedMangaInfo(item);
				DeletedItems.Add(deleted);
			}
			Items.Remove(item);
			SaveChanges();
		}

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

		public MangaInfo GetOrCreateMangaInfo(string containerPath, RecentItemList<MangaInfo> lastAddedCollection, bool saveToDatabase)
		{
			var item = GetByPath(containerPath);
			if (item != null) return item;
			item = MangaInfo.Create(containerPath, saveToDatabase);
			if (saveToDatabase)
			{
				CheckCrcMatches(item);
				Items.Add(item);
				SaveChanges();
				lastAddedCollection?.Add(item);
			}
			return item;

			MangaInfo GetByPath(string path)
			{
				var items = Items.Where(x => path.EndsWith(x.SubPath)).ToArray();
				return items.FirstOrDefault(x => x.FilePath == path);
			}
		}

		/// <summary>
		/// Returns true if CRC should stop being checked.
		/// </summary>
		public bool CheckCrcMatches(MangaInfo preSavedItem)
		{
			var crcMatch = Items.FirstOrDefault(i => i.CRC32 == preSavedItem.CRC32);
			if (crcMatch != null)
			{
				var result = MessageBox.Show(
					@$"Found a CRC32 Match
This: {preSavedItem}
Match: {crcMatch}
Press Yes to delete this and open match.
Press No add this item to database.
Press cancel to skip this message (will default to adding).",
					"Gaman Reader",
					MessageBoxButton.YesNoCancel);
				switch (result)
				{
					case MessageBoxResult.Yes:
						//todo delete this and open match
						break;
					case MessageBoxResult.No:
					case MessageBoxResult.None:
						return false;
					case MessageBoxResult.Cancel:
						//todo
						return true;
					default:
						throw new ArgumentOutOfRangeException(nameof(result), result, "Must be Yes/No/Cancel");
				}
			}
			var deletedCrcMatch = DeletedItems.FirstOrDefault(i => i.CRC32 == preSavedItem.CRC32);
			if (deletedCrcMatch != null)
			{
				var result = MessageBox.Show(
					@$"Found a Deleted CRC32 Match
This: {preSavedItem}
Match: {deletedCrcMatch}
Press Yes to delete this and open match.
Press No add this item to database.
Press cancel to skip this message (will default to adding).",
					"Gaman Reader",
					MessageBoxButton.YesNoCancel);
				switch (result)
				{
					case MessageBoxResult.Yes:
						//todo delete this and open match
						break;
					case MessageBoxResult.No:
					case MessageBoxResult.None:
						return false;
					case MessageBoxResult.Cancel:
						//todo
						return true;
					default:
						throw new ArgumentOutOfRangeException(nameof(result), result, "Must be Yes/No/Cancel");
				}
			}
			return false;
		}

		public int SaveChanges([CallerMemberName] string caller = null)
		{
			var count = base.SaveChanges();
			Debug.WriteLine($"Saved changes to database ({caller}): {count}");
			return count;
		}

		public void AddTag(MangaInfo item, string tag)
		{
			if (item.UserTags.Any(x => x.Tag.ToLower().Equals(tag.ToLower())))
			{
				RemoveTag(item, tag);
				return;
			}
			item.UserTags.Add(new UserTag(item.Id, tag));
			SaveChanges();
			TagPanel.AddTag(item, tag);
			item.OnPropertyChanged(null);
		}

		public void RemoveTag(MangaInfo item, string tag)
		{
			var tagItem = item.UserTags.FirstOrDefault(x => x.Tag.ToLower().Equals(tag.ToLower()));
			if (tagItem == null) return;
			item.UserTags.Remove(tagItem);
			UserTags.Remove(tagItem);
			SaveChanges();
			TagPanel.RemoveTag(item, tag);
			item.OnPropertyChanged(null);
		}

		private class SqliteInterceptCharIndex : IDbCommandInterceptor
		{
			private static readonly Regex ReplaceRegex = new Regex(@"\(CHARINDEX\((.*?),\s?(.*?)\)\)\s*?>\s*?0");

			public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
			{
			}

			public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
			{
			}

			public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
			{
			}

			public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
			{
				ReplaceCharIndexFunc(command);
			}

			public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
			{
			}

			public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
			{
				ReplaceCharIndexFunc(command);
			}

			private void ReplaceCharIndexFunc(DbCommand command)
			{
				bool isMatch = false;
				var text = ReplaceRegex.Replace(command.CommandText, (match) =>
				{
					if (match.Success)
					{
						string paramsKey = match.Groups[1].Value;
						string paramsColumnName = match.Groups[2].Value;
						//replaceParams
						foreach (DbParameter param in command.Parameters)
						{
							if (param.ParameterName == paramsKey.Substring(1))
							{
								param.Value = string.Format("%{0}%", param.Value);
								break;
							}
						}

						isMatch = true;
						return string.Format("{0} LIKE {1}", paramsColumnName, paramsKey);
					}
					else
						return match.Value;
				});
				if (isMatch)
					command.CommandText = text;
			}
		}
	}
}

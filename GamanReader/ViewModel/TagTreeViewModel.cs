using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using GamanReader.Model.Database;
using GamanReader.View;
using JetBrains.Annotations;
using static GamanReader.Model.StaticHelpers;

namespace GamanReader.ViewModel
{
	public class TagTreeViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<TagGroup> TagGroups { get; } = new ObservableCollection<TagGroup>();

		public TagTreePanel.TreeViewTemplateSelector TemplateSelector { get; set; } = new TagTreePanel.TreeViewTemplateSelector(false);

		[UsedImplicitly]
		public string LibraryItemFormat { get; set; } = "";

		public void Initialise(bool loadDatabase)
		{
			if(loadDatabase) Refresh();
		}
		public void Refresh()
		{
			TagGroups.Clear();
			LocalDatabase.AutoTags.Load();
			LocalDatabase.UserTags.Load();
			LocalDatabase.Aliases.Load();
			LocalDatabase.FavoriteTags.Load();
			TagGroups.Add(GetNode("Auto Tags", LocalDatabase.AutoTags.Local.GroupBy(i => i.Tag, StringComparer.OrdinalIgnoreCase)));
			TagGroups.Add(GetNode("User Tags", LocalDatabase.UserTags.Local.GroupBy(i => i.Tag, StringComparer.OrdinalIgnoreCase)));
			TagGroups.Add(GetNode("Aliases", LocalDatabase.Aliases.Local.GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)));
			TagGroups.Add(GetNodeForFavoriteTags());
		}

		private TagGroup GetNode(string header, IEnumerable<IGrouping<string, IndividualTag>> items)
		{
			var node = new TagGroup { Name = header };
			foreach (var set in items)
			{
				var subItem = new TagGroup { Name = set.Key };
				foreach (var item in set) subItem.Items.Add(item.Item);
				subItem.Header = $"{set.Key} ({subItem.Items.Count} items)";
				node.Items.Add(subItem);
			}
			SetHeader(node);
			return node;
		}

		private static TagGroup GetNodeForFavoriteTags()
		{
			var parentNode = new TagGroup { Name = "Favorite Tags" };
			foreach (var favoriteTag in LocalDatabase.FavoriteTags)
			{
				parentNode.Items.Add(CreateNodeForFavoriteTag(favoriteTag));
			}
			SetHeader(parentNode);
			return parentNode;
		}

		private static TagGroup CreateNodeForFavoriteTag(FavoriteTag favoriteTag)
		{
			var subItem = new TagGroup {Name = favoriteTag.Tag};
			var autoItems = LocalDatabase.AutoTags.Local.Where(t => t.Tag == favoriteTag.Tag && t.Item != null);
			foreach (var item in autoItems) subItem.Items.Add(item.Item);
			var userItems = LocalDatabase.UserTags.Local.Where(t => t.Tag == favoriteTag.Tag && t.Item != null);
			foreach (var item in userItems) subItem.Items.Add(item.Item);
			subItem.Header = $"{favoriteTag.Tag} ({subItem.Items.Count} items)";
			return subItem;
		}

		private TagGroup GetNode(string header, IEnumerable<IGrouping<string, Alias>> items)
		{
			var node = new TagGroup { Name = header };
			foreach (var set in items)
			{
				var subItem = new TagGroup { Name = set.Key };
				foreach (var item in set) subItem.Items.Add(item);
				subItem.Header = $"{set.Key} ({subItem.Items.Count} items)";
				node.Items.Add(subItem);
			}
			SetHeader(node);
			return node;
		}

		public void AddFavoriteTag(FavoriteTag tag)
		{
			var favoriteTags = TagGroups[3];
			favoriteTags.Items.Add(CreateNodeForFavoriteTag(tag));
			SetHeader(favoriteTags);
		}

		public void RemoveFavoriteTag(FavoriteTag tag)
		{
			var favoriteTags = TagGroups[3];
			TagGroup thisNode = favoriteTags.Items.Cast<TagGroup>().First(node => node.Name.Equals(tag.Tag));
			favoriteTags.Items.Remove(thisNode);
			SetHeader(thisNode);
		}

		public void AddTag(MangaInfo item, string tag)
		{
			var userGroup = TagGroups[1];
			TagGroup thisNode = userGroup.Items.Cast<TagGroup>().FirstOrDefault(node => node.Name.Equals(tag));
			if (thisNode == null)
			{
				thisNode = new TagGroup
				{
					Name = tag
				};
				userGroup.Items.Add(thisNode);
			}
			thisNode.Items.Add(item);
			SetHeader(thisNode);
		}

		public void RemoveTag(MangaInfo item, string tag)
		{
			var userGroup = TagGroups[1];
			TagGroup thisNode = userGroup.Items.Cast<TagGroup>().First(node => node.Name.Equals(tag));
			thisNode.Items.Remove(item);
			SetHeader(thisNode);
			//todo remove node if empty
		}

		private static void SetHeader(TagGroup node)
		{
			node.Header = node.Items.Count == 1 ? $"{node.Name} ({node.Items.Count} item)" : $"{node.Name} ({node.Items.Count} items)";
		}

		public event PropertyChangedEventHandler PropertyChanged;

		[NotifyPropertyChangedInvocator]
		public virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	public class TagGroup
	{
		public ObservableCollection<object> Items { get; } = new ObservableCollection<object>();

		public string Name { get; set; }

		public string Header { get; set; }
	}
}

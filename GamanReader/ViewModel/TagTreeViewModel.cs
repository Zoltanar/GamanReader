using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GamanReader.Model.Database;
using static GamanReader.Model.StaticHelpers;

namespace GamanReader.ViewModel
{
	public class TagTreeViewModel
	{
		public ObservableCollection<TagGroup> TagGroups { get; } = new ObservableCollection<TagGroup>();

		public TagTreeViewModel()
		{
			Refresh();
		}

		public void Refresh()
		{
			TagGroups.Clear();
			TagGroups.Add(GetNode("Auto Tags", LocalDatabase.AutoTags.ToArray().GroupBy(i => i.Tag, StringComparer.OrdinalIgnoreCase)));
			TagGroups.Add(GetNode("User Tags", LocalDatabase.UserTags.ToArray().GroupBy(i => i.Tag, StringComparer.OrdinalIgnoreCase)));
			TagGroups.Add(GetNode("Aliases", LocalDatabase.Aliases.ToArray().GroupBy(i => i.Name, StringComparer.OrdinalIgnoreCase)));
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

		void SetHeader(TagGroup node)
		{
			node.Header = node.Items.Count == 1 ? $"{node.Name} ({node.Items.Count} item)" : $"{node.Name} ({node.Items.Count} items)";
		}

	}
	public class TagGroup
	{
		public ObservableCollection<object> Items { get; } = new ObservableCollection<object>();

		public string Name { get; set; }

		public string Header { get; set; }
	}
}

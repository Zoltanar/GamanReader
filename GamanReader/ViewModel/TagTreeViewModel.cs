using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using GamanReader.Model.Database;
using static GamanReader.Model.StaticHelpers;

namespace GamanReader.ViewModel
{
	public class TagTreeViewModel
	{
		public BindingList<TreeViewItem> TagGroups { get; } = new BindingList<TreeViewItem>();

		public TagTreeViewModel()
		{
			Refresh();
		}

		public void Refresh()
		{
			TagGroups.Clear();
			TagGroups.Add(GetNode("Auto Tags", LocalDatabase.AutoTags.ToArray().GroupBy(i => i.Tag, StringComparer.OrdinalIgnoreCase)));
			TagGroups.Add(GetNode("User Tags", LocalDatabase.UserTags.ToArray().GroupBy(i => i.Tag, StringComparer.OrdinalIgnoreCase)));
		}

		private TreeViewItem GetNode(string header, IEnumerable<IGrouping<string, IndividualTag>> items)
		{
			var node = new TreeViewItem();
			foreach (var set in items)
			{
				var subItem = new TreeViewItem { Tag = set.Key };
				foreach (var item in set) subItem.Items.Add(item.Item);
				subItem.Header = $"{set.Key} ({subItem.Items.Count} items)";
				node.Items.Add(subItem);
			}
			SetHeader(node);
			return node;
		}

		public void AddTag(MangaInfo item, string tag)
		{
			var userGroup = TagGroups[1];
			TreeViewItem thisNode = userGroup.Items.Cast<TreeViewItem>().FirstOrDefault(node => node.Tag.Equals(tag));
			if (thisNode == null)
			{
				thisNode = new TreeViewItem
				{
					Tag = tag
				};
				userGroup.Items.Add(thisNode);
			}
			thisNode.Items.Add(item);
			SetHeader(thisNode);
		}

		public void SetHeader(TreeViewItem node)
		{
			node.Header = node.Items.Count == 1 ? $"{node.Tag} ({node.Items.Count} item)" : $"{node.Tag} ({node.Items.Count} items)";
		}
	}
}

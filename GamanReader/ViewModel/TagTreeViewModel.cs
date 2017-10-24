using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GamanReader.Model;
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
			TagGroups.Add(GetNode("Event", LocalDatabase.Items.GroupBy(i => i.Event)));
			TagGroups.Add(GetNode("Group", LocalDatabase.Items.GroupBy(i => i.Group)));
			TagGroups.Add(GetNode("Artist", LocalDatabase.Items.GroupBy(i => i.Artist)));
			TagGroups.Add(GetNode("Parody", LocalDatabase.Items.GroupBy(i => i.Parody)));
			TagGroups.Add(GetNode("Subber", LocalDatabase.Items.GroupBy(i => i.Subber)));
			TagGroups.Add(GetNode("Auto Tags", LocalDatabase.AutoTags.ToArray().GroupBy(i => i.Tag,StringComparer.OrdinalIgnoreCase)));
		}
		
		private TreeViewItem GetNode<T>(string header, IEnumerable<IGrouping<string, T>> items)
		{
			var node = new TreeViewItem { Header = header };
			foreach (var set in items)
			{
				var subItem = new TreeViewItem { Header = set.Key };
				foreach (var item in set) subItem.Items.Add(item);
				subItem.Header += $" ({subItem.Items.Count} items)";
				node.Items.Add(subItem);
			}
			node.Header += $" ({node.Items.Count} items)";
			return node;
		}

	}
}

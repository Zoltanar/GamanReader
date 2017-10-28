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
			var node = new TreeViewItem { Header = header };
			foreach (var set in items)
			{
				var subItem = new TreeViewItem { Header = set.Key };
				foreach (var item in set) subItem.Items.Add(item.Item);
				subItem.Header += $" ({subItem.Items.Count} items)";
				node.Items.Add(subItem);
			}
			node.Header += $" ({node.Items.Count} items)";
			return node;
		}

	}
}

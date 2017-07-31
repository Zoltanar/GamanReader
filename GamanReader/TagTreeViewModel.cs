using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GamanReader
{
	public class TagTreeViewModel
	{
		public ObservableCollection<TagGroup> TagGroups { get; set; } = new ObservableCollection<TagGroup>(GetTagGroups());
		
		private static IEnumerable<TagGroup> GetTagGroups()
		{
			var tagDatabase = new TagDatabase();
			var tagGroups = tagDatabase.Tags.GroupBy(i => i.Tag);
			var groups = new List<TagGroup>();
			foreach (var group in tagGroups)
			{
				if (!group.Any()) continue;
				var tagNode = new TagGroup() { Name = group.Key };
				foreach (var tag in group) tagNode.Children.Add(tag.TaggedItem);
				groups.Add(tagNode);
			}
			return groups;
		}
	}

	public class TagGroup
	{
		public string Name { get; set; }
		public ICollection<TaggedItem> Children { get; set; } = new HashSet<TaggedItem>();		
	}	
}

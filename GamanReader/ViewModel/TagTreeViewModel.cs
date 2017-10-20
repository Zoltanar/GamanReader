using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using GamanReader.Model;
using GamanReader.Model.Database;

namespace GamanReader.ViewModel
{
	public class TagTreeViewModel
	{
		public BindingList<TagGroup> TagGroups { get; } = new BindingList<TagGroup>();

		public TagTreeViewModel()
		{
			Refresh();
		}

		public void Refresh()
		{
			TagGroups.Clear();
#if DEBUG
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				TagGroups.SetRange(GetTagGroupsMockup());
				return;
			}
#endif
			var tagGroups = StaticHelpers.LocalDatabase.Tags.GroupBy(i => i.Tag);
			var groups = new List<TagGroup>();
			foreach (var group in tagGroups)
			{
				if (!group.Any()) continue;
				var tagNode = new TagGroup { Name = group.Key };
				foreach (var tag in group) tagNode.Children.Add(tag.TaggedItem);
				groups.Add(tagNode);
			}
			TagGroups.SetRange(groups);
		}

#if DEBUG
		private List<TagGroup> GetTagGroupsMockup()
		{
			var rnd = new Random();
			var inGroups = new List<TagGroup>();
				for (int count = 0; count < rnd.Next(10); count++)
				{
					var tg = new TagGroup { Name = rnd.Next(7777).ToString("x") };
					for (int cCount = 0; cCount < rnd.Next(13); cCount++)
					{
						var tgi = new TaggedItem { Path = rnd.Next(80569).ToString("x"), Tags = new HashSet<IndividualTag>() };
						if (rnd.Next(3) == 0) tgi.Tags.Add(new IndividualTag { Tag = "favorite" });
						tg.Children.Add(tgi);
					}
					inGroups.Add(tg);
				}
				return inGroups;
			}
#endif
	}

	public class TagGroup
	{
		public string Name { get; set; }
		public ICollection<TaggedItem> Children { get; set; } = new HashSet<TaggedItem>();
	}
}

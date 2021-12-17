using System.Collections.Generic;
using System.Linq;
// ReSharper disable VirtualMemberCallInConstructor
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global

namespace GamanReader.Model.Database
{
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

		public void AddTags(IEnumerable<string> tags)
		{
			foreach (var iTag in tags)
			{
				if (Tags.Contains(iTag.ToLower())) continue;
				AliasTags.Add(new AliasTag{AliasId = Id, Tag = iTag.ToLower()});
			}
		}

		public override string ToString() => Name;
	}
	
	public class AliasTag
	{
		public int Id { get; set; }
		public int AliasId { get; set; }
		public string Tag { get; set; }
		public virtual Alias Alias { get; set; }
	}
}
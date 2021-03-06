﻿using System.ComponentModel.DataAnnotations;

namespace GamanReader.Model.Database
{
	public abstract class IndividualTag
	{
		public int Id { get; set; }
		public long ItemId { get; set; }
		public string Tag { get; set; }
		public virtual MangaInfo Item { get; set; }

		protected IndividualTag(string tag) { Tag = tag; }

		protected IndividualTag(long itemId, string tag) { ItemId = itemId; Tag = tag; }

		protected IndividualTag() { }

		public override string ToString() => Tag;
	}

	public class AutoTag : IndividualTag
	{
		public AutoTag(string tag) : base(tag) { }
		public AutoTag() { }
	}

	public class UserTag : IndividualTag
	{
		public UserTag(long itemId, string tag) : base(itemId, tag) { }
		public UserTag() { }
	}

	public class FavoriteTag
	{
		[Key]
		public string Tag { get; set; }

		public FavoriteTag(string tag) => Tag = tag;
		public FavoriteTag() { }
	}
}
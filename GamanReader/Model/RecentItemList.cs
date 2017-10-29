﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GamanReader.Model
{
	public class RecentItemList<T>
	{
		public readonly int Size;
		public readonly BindingList<T> Items;
		public RecentItemList(int size = 25, IEnumerable<T> items = null)
		{
			Size = size;
			Items = new BindingList<T>();
			if (items != null) Items.AddRange(items);
		}
		public void Add(T item)
		{
			var foundItem = Items.FirstOrDefault(x => x.Equals(item));
			//probably won't work well with value types
			if (foundItem != null)
			{
				Items.Remove(foundItem);
				Items.Insert(0, item);
				return;
			}
			if (Items.Count == Size) Items.RemoveAt(Items.Count - 1);
			Items.Insert(0, item);
		}
	}
}

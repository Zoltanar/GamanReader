using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GamanReader.Model
{
	public class RecentItemList<T>
	{
		private readonly int _size;
		private readonly Func<IEnumerable<T>> _getItems;
		public readonly ObservableCollection<T> Items;
		public RecentItemList(int size = 25, Func<IEnumerable<T>> getItems = null)
		{
			_size = size;
			_getItems = getItems;
			Items = new ObservableCollection<T>();
			if (getItems == null) return;
			Items.AddRange(getItems());
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
			if (Items.Count == _size) Items.RemoveAt(Items.Count - 1);
			Items.Insert(0, item);
		}

		public void Reset()
		{
			if (_getItems == null) return;
			Items.SetRange(_getItems());
		}
	}
}

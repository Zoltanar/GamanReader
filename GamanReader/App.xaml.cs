using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using GamanReader.Model;
using GamanReader.Model.Database;
using GamanReader.ViewModel;

namespace GamanReader
{
	public partial class App
	{
		private bool _showingError;
		private static MainViewModel MainModel
		{
			get
			{
				Debug.Assert(Current.MainWindow != null, "App.Current.MainWindow != null");
				return (MainViewModel)Current.MainWindow.DataContext;
			}
		}

		private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (_showingError)
			{
				e.Handled = true;
				return;
			}
			_showingError = true;
			try
			{
				var message = $"Press Yes to continue or No to Exit.{Environment.NewLine}{e.Exception}";
				var response = MessageBox.Show(message.Substring(0, Math.Min(message.Length, 1000)),
					"Unhandled Exception", MessageBoxButton.YesNo);
				if (response == MessageBoxResult.No) Shutdown(-1);
			}
			finally
			{
				_showingError = false;
				e.Handled = true;
			}
		}

		public void DeleteItemAndAddToDeleted(object sender, RoutedEventArgs e)
		{
			var menuItem = (FrameworkElement)sender;
			var item = GetMangaItem(menuItem);
			if (item == null || item.IsDeleted) return;
			var success = MainModel.RemoveItemFromDb(item, true, true);
			if (success)
			{
				//MainModel.OnPropertyChanged(null);
			}
		}

		private void DeleteItemWithoutAddingToDeleted(object sender, RoutedEventArgs e)
		{
			var menuItem = (FrameworkElement)sender;
			var item = GetMangaItem(menuItem);
			if (item == null) return;
			var success = MainModel.RemoveItemFromDb(item, false, true);
			if (success) MainModel.LibraryItems.Remove(item);
		}

		private void RemoveItemAndAddToDeleted(object sender, RoutedEventArgs e)
		{
			var menuItem = (FrameworkElement)sender;
			var item = GetMangaItem(menuItem);
			if (item == null) return;
			var success = MainModel.RemoveItemFromDb(item, true, false);
			if (success) item.OnPropertyChanged(nameof(IMangaItem.IsDeleted));//MainModel.LibraryItems.Remove(item);
		}

		private void RemoveItemWithoutAddingToDeleted(object sender, RoutedEventArgs e)
		{
			var menuItem = (FrameworkElement)sender;
			var item = GetMangaItem(menuItem);
			if (item == null) return;
			var success = MainModel.RemoveItemFromDb(item, false, false);
			if (success) MainModel.LibraryItems.Remove(item);
		}

		private void BrowseToItemLocation(object sender, RoutedEventArgs e)
		{
			var menuItem = (FrameworkElement)sender;
			var item = GetMangaItem(menuItem);
			if (item == null) return;
			if (!item.IsFolder && item.Exists())
			{
				Process.Start("explorer", $"/select,  \"{item.FilePath}\"");
				return;
			}
			var location = item.IsFolder ? new DirectoryInfo(item.FilePath) : Directory.GetParent(item.FilePath);
			while (location != null)
			{
				if (location.Exists)
				{
					break;
				}
				location = location.Parent;
			}
			if (location == null) throw new DirectoryNotFoundException($"Could not find any existing directory in path '{item.FilePath}'");
			Process.Start("explorer", $"\"{location.FullName}\"");
		}

		private MangaInfo GetMangaItem(FrameworkElement menuItem)
		{
			var mangaItem = FindDataContextFromParent<MangaInfo>(menuItem);
			return mangaItem ?? MainModel.MangaInfo;
		}

		private static T FindDataContextFromParent<T>(FrameworkElement child)
		{
			while (true)
			{
				if (child.DataContext is T typedDataContext) return typedDataContext;
				//get parent item
				var parentObject = VisualTreeHelper.GetParent(child);
				switch (parentObject)
				{
					//we've reached the end of the tree
					case null:
						return default;
					//check if the parent matches the type we're looking for
					default:
						child = (FrameworkElement)parentObject;
						break;
				}
			}
		}

		private void ItemContextMenu_OnOpened(object sender, RoutedEventArgs e)
		{
			var contextMenu = (ContextMenu)sender;
			if (!(contextMenu.DataContext is MangaInfo item)) return;
			SetupSearchMenuItem(contextMenu, item);
			SetupFavoriteTagMenuItem(contextMenu, item);
		}

		private void SetupSearchMenuItem(ContextMenu contextMenu, MangaInfo item)
		{
			var searchMenuItem = contextMenu.Items.Cast<MenuItem>().Single(mi => mi.Header.Equals("Search"));
			searchMenuItem.Items.Clear();
			var nameMenuItem = new MenuItem { Header = item.Name, Tag = item.Name };
			nameMenuItem.Click += SearchByDebugTagString;
			searchMenuItem.Items.Add(nameMenuItem);
			foreach (var tag in item.AutoTags)
			{
				var tagMenuItem = new MenuItem { Header = tag.Tag, Tag = tag };
				tagMenuItem.Click += SearchByDebugTagString;
				searchMenuItem.Items.Add(tagMenuItem);
			}
		}

		private void SetupFavoriteTagMenuItem(ContextMenu contextMenu, MangaInfo item)
		{
			var addTagMenuItem = contextMenu.Items.Cast<MenuItem>().Single(mi => mi.Header.Equals("Add Tag to Favorites"));
			addTagMenuItem.Items.Clear();
			foreach (var tag in item.AutoTags)
			{
				var tagMenuItem = new MenuItem { Header = tag.Tag, Tag = tag };
				tagMenuItem.Click += (_, _) => StaticHelpers.LocalDatabase.AddFavoriteTag(tag.Tag);
				addTagMenuItem.Items.Add(tagMenuItem);
			}
		}

		private void SearchByDebugTagString(object sender, RoutedEventArgs e)
		{
			var menuItem = (MenuItem)sender;
			var tag = menuItem.Tag;
			switch (tag)
			{
				case string searchText:
					MainModel.Search(searchText,false);
					break;
				case AutoTag autoTag:
					MainModel.SearchTags(new HashSet<string> { autoTag.Tag }, false);
					break;
				default:
					throw new InvalidOperationException();
			}
		}

		private void ItemTextBlockLoaded(object sender, RoutedEventArgs e)
		{
			var textBlock = (TextBlock)sender;
			if (textBlock.DataContext is not IMangaItem item)
			{
				textBlock.Inlines.Clear();
				return;
			}
			var format = MainModel.LibraryItemFormat; // values.Length > 1 ? values[1] as string : null;
			var fileCountRun = new Bold(new Run(item.FileCountString));
			var prefixLines = format switch
			{
				"T" => new Inline[] { new Italic(new Run($"[{item.TimesBrowsed:00}]")), fileCountRun },
				"A" => new Inline[] { new Italic(new Run($"[{item.DateAdded:yyyyMMdd}]")), fileCountRun },
				_ => new Inline[] { fileCountRun }
			};
			foreach (var inline in prefixLines.Concat(item.GetDescriptiveTitle()))
			{
				//var clone = ((object)inline).Clo
				if (item.IsFavorite && inline is Hyperlink link)
				{
					var firstChild = link.Inlines.FirstInline;
					if (firstChild.Foreground == Brushes.Pink) firstChild.Foreground = Brushes.Red;
				}
				textBlock.Inlines.Add(inline);
			}
		}

		private void ItemTextBlockLoaded2(object sender, DependencyPropertyChangedEventArgs e)
		{
			ItemTextBlockLoaded(sender, null);
		}
	}
}

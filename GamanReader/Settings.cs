using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;

namespace GamanReader
{
	public static class Settings
	{
		private static SettingsItem _instance = new SettingsItem();

		static Settings()
		{
			Load();
		}

		public static List<string> RecentFolders { get => _instance.RecentFolders; }
		public static int RecentListSize
		{
			get => _instance.RecentListSize;
			set
			{
				if (_instance.RecentListSize == value) return;
				_instance.RecentListSize = value;
				Save();
			}
		}

		public static string LibraryFolder
		{
			get => _instance.LibraryFolder;
			set
			{
				if (_instance.LibraryFolder == value) return;
				_instance.LibraryFolder = value;
				Save();
			}
		}

		public static void Save(ObservableCollection<string> items)
		{
			_instance.RecentFolders = items.ToList();
			try
			{
				File.WriteAllText(StaticHelpers.SettingsJson, JsonConvert.SerializeObject(_instance,Formatting.Indented));
			}
			catch
			{
				//TODO log error
			}
		}
		private static void Save()
		{
			try
			{
				File.WriteAllText(StaticHelpers.SettingsJson, JsonConvert.SerializeObject(_instance, Formatting.Indented));
			}
			catch
			{
				//TODO log error
			}
		}

		public static void Load()
		{
			try
			{
				var settings = JsonConvert.DeserializeObject<SettingsItem>(File.ReadAllText(StaticHelpers.SettingsJson));
				if (settings != null) _instance = settings;
			}
			catch
			{
				//TODO log error
			}
		}

		private class SettingsItem
		{
			public List<string> RecentFolders { get; set; }
			public int RecentListSize { get; set; }
			public string LibraryFolder { get; set; }

			public SettingsItem()
			{
				RecentListSize = 25;
				RecentFolders = new List<string>();
				LibraryFolder = null;
			}
		}		
	}
}

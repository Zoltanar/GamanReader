using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using static GamanReader.Model.StaticHelpers;

namespace GamanReader.Model
{
	public static class Settings
	{
		private static SettingsItem _instance = new SettingsItem();

		static Settings()
		{
			Directory.CreateDirectory(StoredDataFolder);
			Directory.CreateDirectory(TempFolder);
			Load();
		}

		public static List<string> RecentFolders => _instance.RecentFolders;

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
				File.WriteAllText(SettingsJson, JsonConvert.SerializeObject(_instance,Formatting.Indented));
			}
			catch(Exception ex)
			{
				LogToFile("Failed to save settings.", ex);
			}
		}
		private static void Save()
		{
			try
			{
				File.WriteAllText(SettingsJson, JsonConvert.SerializeObject(_instance, Formatting.Indented));
			}
			catch (Exception ex)
			{
				LogToFile("Failed to save settings.", ex);
			}
		}

		public static void Load()
		{
			try
			{
				var settings = JsonConvert.DeserializeObject<SettingsItem>(File.ReadAllText(SettingsJson));
				if (settings != null) _instance = settings;
			}
			catch(Exception ex)
			{
				LogToFile("Failed to load settings.", ex);
				_instance = new SettingsItem();
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

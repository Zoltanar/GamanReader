using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GamanReader
{
	static class StaticHelpers
	{
#if DEBUG
		public const string TempFolder = "..\\Release\\Stored Data\\Temp";
		public const string SettingsJson = "..\\Release\\Stored Data\\settings.json";
		public const string StoredDataFolder = "..\\Release\\Stored Data";
#else
        public const string TempFolder = "Stored Data\\Temp";
        public const string SettingsJson = "Stored Data\\settings.json";
        public const string StoredDataFolder = "Stored Data";
#endif
		public const string ProgramName = "GamanReader";
		private const string AllowedFormatsJson = "allowedformats.json";

		public static TagDatabase TagDatabase { get; private set; }

		public static string[] AllowedFormats { get; private set; }

		static StaticHelpers()
		{
			try
			{
				AllowedFormats = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(AllowedFormatsJson));
				TagDatabase = new TagDatabase();
			}
			catch (Exception)
			{
				//TODO log error
			}
		}

		public static ImageSource GetFavoritesIcon()
		{
			/*Image finalImage = new Image();
BitmapImage logo = new BitmapImage();
			logo.BeginInit();
			logo.UriSource = new Uri("pack://application:,,,/AssemblyName;component/Resources/goldstar");
			logo.EndInit();
finalImage.Source = logo;*/
			/*var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
			string packUri = $"pack://application:,,,/{assemblyName};component/Resources/goldstar.jpg";
			var source = new ImageSourceConverter().ConvertFromString(packUri) as ImageSource;*/
			var uriSource = new Uri(@"/GamanReader;component/Resources/favorites.ico", UriKind.Relative);
			var source = new BitmapImage(uriSource);
			return source;
		}

		public static void AddTag(string itempath, bool isFolder, string tag)
		{
			var hash = isFolder ? new byte[0] : GetHash(itempath);
			var taggedItem = TagDatabase.TaggedItems.SingleOrDefault(i => isFolder ? i.Path == itempath : i.MD5Hash == hash);
			if (taggedItem == null)
			{
				taggedItem = new TaggedItem
				{
					Path = itempath,
					IsFolder = isFolder,
					MD5Hash = hash,
					Tags = new List<IndividualTag>()
				};
				TagDatabase.TaggedItems.Add(taggedItem);
			}
			taggedItem.Tags.Add(new IndividualTag { Tag = tag });
			TagDatabase.SaveChanges();
		}

		private static byte[] GetHash(string itempath)
		{
			using (var stream = File.OpenRead(itempath))
			{
				using (var md5 = MD5.Create())
				{
					return md5.ComputeHash(stream);
				}
			}
		}

		public static bool CtrlIsDown()
		{
			return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
		}

	}
}

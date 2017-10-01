using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace GamanReader.Model
{
	public static class StaticHelpers
	{
#if DEBUG
		public const string StoredDataFolder = "..\\Release\\Stored Data";
#else
        public const string StoredDataFolder = "Stored Data";
#endif
		public const string TempFolder = StoredDataFolder + "\\Temp";
		public const string SettingsJson = StoredDataFolder + "\\settings.json";
		public const string LogFile = StoredDataFolder + "\\message.log";

		public const string ProgramName = "GamanReader";
		private const string AllowedFormatsJson = "allowedformats.json";

		public static TagDatabase TagDatabase { get; }

		public static string[] AllowedFormats { get; }

		static StaticHelpers()
		{
			try
			{
				AllowedFormats = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(AllowedFormatsJson));
				TagDatabase = new TagDatabase();
				Directory.CreateDirectory(StoredDataFolder);
			}
			catch (Exception ex)
			{
				LogToFile($"Failed to load {AllowedFormatsJson} or TagDatabase.", ex);
			}
		}

		public static ImageSource GetFavoritesIcon()
		{
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
		/// <summary>
		/// Print message to Debug and write it to log file.
		/// </summary>
		/// <param name="message">Message to be written</param>
		public static void LogToFile(string message)
		{
			LogToFile(new[] { message });
		}

		/// <summary>
		/// Print exception to Debug and write it to log file.
		/// </summary>
		/// <param name="header">Human-given location or reason for error</param>
		/// <param name="exception">Exception to be written to file</param>
		public static void LogToFile(string header, Exception exception)
		{
			LogToFile(new[] { header, exception.Message, exception.StackTrace });
		}

		private static void LogToFile(ICollection<string> lines)
		{
			foreach (var line in lines) Debug.Print(line);

			int counter = 0;
			while (IsFileLocked(new FileInfo(LogFile)))
			{
				counter++;
				if (counter > 5) throw new IOException("Logfile is locked!");
				Thread.Sleep(25);
			}
			using (var writer = new StreamWriter(LogFile, true))
			{
				foreach (var line in lines) writer.WriteLine(line);
			}
		}

		/// <summary>
		/// Check if file is locked,
		/// </summary>
		/// <param name="file">File to be checked</param>
		/// <returns>Whether file is locked</returns>
		public static bool IsFileLocked(FileInfo file)
		{
			FileStream stream = null;

			try
			{
				stream = file.Exists ? file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None) : file.Create();
			}
			catch (IOException)
			{
				return true;
			}
			finally
			{
				stream?.Close();
			}
			return false;
		}

		/// <summary>
		/// Get substring between indexes, including the index locations.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="startIndex"></param>
		/// <param name="endIndex"></param>
		/// <returns></returns>
		public static string BetweenIndexes(this string input, int startIndex, int endIndex) => input.Substring(startIndex, endIndex - startIndex+1);
	}
}

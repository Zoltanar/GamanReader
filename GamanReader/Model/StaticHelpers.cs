using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GamanReader.Model.Database;
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

		public static readonly string[] RecognizedContainers = { ".zip", ".rar" };

		public static GamanDatabase LocalDatabase { get; }

		public static string[] AllowedFormats { get; }
		public static Random Random { get; } = new Random();

		static StaticHelpers()
		{
			try
			{
				AllowedFormats = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(AllowedFormatsJson));
				LocalDatabase = new GamanDatabase();
				if (!LocalDatabase.Libraries.Any(x => x.Id == 1))
				{
					LocalDatabase.Libraries.Add(new LibraryFolder(""));
					LocalDatabase.SaveChanges();
				}
					Directory.CreateDirectory(StoredDataFolder);
			}
			catch (Exception ex)
			{
				LogToFile($"Failed to load {AllowedFormatsJson} or TagDatabase.", ex);
			}
		}

		public static MangaInfo GetOrCreateMangaInfo(string containerPath)
		{
			var item = GetByPath(containerPath);
			if (item != null) return item;
			var preSavedItem = MangaInfo.Create(containerPath);
			LocalDatabase.Information.Add(preSavedItem);
			LocalDatabase.SaveChanges();
			item = GetByPath(containerPath);
			return item;

			MangaInfo GetByPath(string path)
			{

				var items = LocalDatabase.Information.Where(x => path.EndsWith(x.SubPath)).ToArray();
				return items.FirstOrDefault(x => x.FilePath == path);
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
			var hash = isFolder ? new byte[0] : new FileInfo(itempath).GetMd5Hash();
			var taggedItem = LocalDatabase.TaggedItems.SingleOrDefault(i => isFolder ? i.Path == itempath : i.MD5Hash == hash);
			if (taggedItem == null)
			{
				taggedItem = new TaggedItem
				{
					Path = itempath,
					IsFolder = isFolder,
					MD5Hash = hash,
					Tags = new List<IndividualTag>()
				};
				LocalDatabase.TaggedItems.Add(taggedItem);
			}
			taggedItem.Tags.Add(new IndividualTag { Tag = tag });
			LocalDatabase.SaveChanges();
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
			while (new FileInfo(LogFile).IsLocked())
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
		public static bool IsLocked(this FileInfo file)
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

		public static byte[] GetMd5Hash(this FileInfo file)
		{
			try
			{

				using (var stream = file.OpenRead())
				{
					using (var md5 = MD5.Create())
					{
						return md5.ComputeHash(stream);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		/// <summary>
		/// Get substring between indexes, including the index locations.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="startIndex"></param>
		/// <param name="endIndex"></param>
		/// <returns></returns>
		public static string BetweenIndexes(this string input, int startIndex, int endIndex) => input.Substring(startIndex, endIndex - startIndex + 1);

		/// <summary>
		/// Pause RaiseListChangedEvents and add items then call the event when done adding.
		/// </summary>
		public static void AddRange<T>(this BindingList<T> list, IEnumerable<T> items)
		{
			if (items == null) return;
			list.RaiseListChangedEvents = false;
			foreach (var item in items) list.Add(item);
			list.RaiseListChangedEvents = true;
			list.ResetBindings();
		}

		/// <summary>
		/// Pause RaiseListChangedEvents, clear list and add items, then call ResetBindings event.
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public static void SetRange<T>(this BindingList<T> list, IEnumerable<T> items)
		{
			list.RaiseListChangedEvents = false;
			list.Clear();
			foreach (var item in items) list.Add(item);
			list.RaiseListChangedEvents = true;
			list.ResetBindings();
		}

		/// <summary>
		/// Returns path stored in a lnk shortcut.
		/// </summary>
		/// <param name="containerPath">Full path of shortcut file.</param>
		public static string GetPathFromShortcut(string containerPath)
		{
			// IWshRuntimeLibrary is in the COM library "Windows Script Host Object Model"
			var shell = new IWshRuntimeLibrary.WshShell();
			var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(containerPath);
			return shortcut.TargetPath;
		}
	}
}

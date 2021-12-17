using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GamanReader.Model.Database;
using GamanReader.View;
using IWshRuntimeLibrary;
using JetBrains.Annotations;
using Newtonsoft.Json;
using File = System.IO.File;

namespace GamanReader.Model
{
	public static class StaticHelpers
	{
		public static readonly string StoredDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Gaman Reader", "Stored Data");
		public static readonly string TempFolder = StoredDataFolder + "\\Temp";
		public static readonly string ThumbFolder = StoredDataFolder + "\\Thumb";
		public static readonly string LogFile = StoredDataFolder + "\\message.log";
		public const string LoadFailedImage = "loadfailedn.png";

		public const string ProgramName = "GamanReader";
		private const string AllowedFormatsJson = "allowedformats.json";

		public static readonly string[] RecognizedContainers = {".zip", ".rar"};

		public const string FavoriteTagString = "favorite";
		public const string BlacklistedTagString = "blacklisted";

		[NotNull] public static GamanDatabase LocalDatabase { get; }

		public static string[] AllowedFormats { get; }
		public static MainWindow MainModel { get; set; }

		static StaticHelpers()
		{
			LocalDatabase = new GamanDatabase();
			LocalDatabase.DefaultLibrary = LocalDatabase.Libraries.FirstOrDefault(x => x.Id == 1);
			if (LocalDatabase.DefaultLibrary == null)
			{
				LocalDatabase.DefaultLibrary = new LibraryFolder("");
				LocalDatabase.Libraries.Add(LocalDatabase.DefaultLibrary);
				LocalDatabase.SaveChanges();
			}
			Directory.CreateDirectory(StoredDataFolder);
			Directory.CreateDirectory(TempFolder);
			Directory.CreateDirectory(ThumbFolder);
			try
			{
				AllowedFormats = JsonConvert.DeserializeObject<string[]>(File.ReadAllText(AllowedFormatsJson));
			}
			catch (Exception ex)
			{
				LogToFile($"Failed to load {AllowedFormatsJson}", ex);
			}
		}


		public static string GetNoTagName(string name)
		{
			string noTagName = string.Empty;
			int inBrackets = 0;
			foreach (char c in name)
			{
				switch (c)
				{
					case '[':
					case '(':
					case '{':
						inBrackets++;
						break;
					case ']':
					case ')':
					case '}':
						inBrackets--;
						if (inBrackets == 0)
						{
							if (!string.IsNullOrWhiteSpace(noTagName)) return noTagName.Trim().ToLowerInvariant();
							noTagName = string.Empty;
						}
						break;
					default:
						if (inBrackets == 0) noTagName += c;
						break;
				}
			}
			return !string.IsNullOrWhiteSpace(noTagName) ? noTagName.Trim().ToLowerInvariant() : string.Empty;
		}

		public static int NumberBetween(int min, int max, int value)
		{
			return Math.Min(Math.Max(value, min), max);
		}

		public static Expression<Func<MangaInfo, bool>> IsFavorited() => mi => mi.UserTags.Any(t => t.Tag.ToLower().Equals(FavoriteTagString));

		public static Expression<Func<MangaInfo, bool>> IsBlacklisted() => mi => mi.UserTags.Any(t => t.Tag.ToLower().Equals(BlacklistedTagString));

		public static ImageSource GetFavoritesIcon()
		{
			var uriSource = new Uri(@"/GamanReader;component/Resources/favorites.ico", UriKind.Relative);
			var source = new BitmapImage(uriSource);
			return source;
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
			LogToFile(new[] {message});
		}

		/// <summary>
		/// Print exception to Debug and write it to log file.
		/// </summary>
		/// <param name="header">Human-given location or reason for error</param>
		/// <param name="exception">Exception to be written to file</param>
		public static void LogToFile(string header, Exception exception)
		{
			LogToFile(new[] {header, exception.Message, exception.StackTrace});
		}

		private static void LogToFile(ICollection<string> lines)
		{
			foreach (var line in lines) Console.WriteLine(line);
			int counter = 0;
			while (new FileInfo(LogFile).IsLocked())
			{
				counter++;
				if (counter > 5) throw new IOException("Logfile is locked!");
				Thread.Sleep(25);
			}
			using var writer = new StreamWriter(LogFile, true);
			foreach (var line in lines) writer.WriteLine(line);
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

		public static void AddRange<T>(this ObservableCollection<T> list, IEnumerable<T> items)
		{
			foreach (var item in items) list.Add(item);
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

		public static void SetRange<T>(this ObservableCollection<T> list, IEnumerable<T> items)
		{
			list.Clear();
			foreach (var item in items) list.Add(item);
		}

		/// <summary>
		/// Returns path stored in a lnk shortcut.
		/// </summary>
		/// <param name="containerPath">Full path of shortcut file.</param>
		public static string GetPathFromShortcut(string containerPath)
		{
			// IWshRuntimeLibrary is in the COM library "Windows Script Host Object Model"
			var shell = new WshShell();
			var shortcut = (IWshShortcut) shell.CreateShortcut(containerPath);
			return shortcut.TargetPath;
		}

		public static bool UserIsSure(string message = "Are you sure?")
		{
			return MessageBox.Show(message, "Gaman Reader - Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
		}

		public static T RunWithRetries<T>(Func<T> action, Func<Exception, bool> exceptionAllowed, int maxTries, int? timeBetweenTries, int? timeoutMS)
		{
			int tries = 0;
			Exception exOuter;
			var watch = Stopwatch.StartNew();
			do
			{
				try
				{
					return action();
				}
				catch (Exception ex)
				{
					exOuter = ex;
					tries++;
					if (!exceptionAllowed(ex) 
					    || tries > maxTries 
					    || timeoutMS.HasValue && watch.ElapsedMilliseconds > timeoutMS) throw;
					if (timeBetweenTries.HasValue) Thread.Sleep(timeBetweenTries.Value);
				}
			} while (tries < maxTries);
			throw exOuter;
		}
		
		public static string ToSeconds(this TimeSpan ts)
		{
			return $"{ts.TotalSeconds:0.###}";
		}

		public static List<Inline> GetTbInlines(string name)
		{
			var list = new List<Inline>();
			int inBrackets = 0;
			var curHp = new Hyperlink();
			curHp.Click += MainModel.TagLinkClicked;
			var curRun = new Run();
			foreach (char c in name)
			{
				switch (c)
				{
					case '[':
					case '(':
					case '{':
						if (inBrackets == 0 && curRun.Text.Length > 0)
						{
							list.Add(curRun);
							curRun = new Run();
						}
						else if (inBrackets > 0 && curRun.Text.Length > 0)
						{
							AddTagRun(curHp.Inlines, ref curRun);
							list.Add(curHp);
							curHp = new Hyperlink { Tag = new List<Hyperlink>() };
							curHp.Click += MainModel.TagLinkClicked;
						}
						curRun.Text += c;
						inBrackets++;
						break;
					case ']':
					case ')':
					case '}':
						curRun.Text += c;
						if (inBrackets == 0 && curRun.Text.Length > 1)
						{
							AddTagRun(list, ref curRun);
						}
						else if (inBrackets > 0 && curRun.Text.Length > 1)
						{
							AddTagRun(curHp.Inlines, ref curRun);
							for (int index = list.Count - 1; index >= list.Count - (inBrackets - 1); index--)
							{
								var item = list[index];
								if (item is Hyperlink hpItem) ((List<Hyperlink>)curHp.Tag).Add(hpItem);
							}
							list.Add(curHp);
							curHp = new Hyperlink { Tag = new List<Hyperlink>() };
							curHp.Click += MainModel.TagLinkClicked;
						}
						inBrackets--;
						break;
					default:
						curRun.Text += c;
						break;
				}
			}
			if (curRun.Text.Length > 0) list.Add(curRun);
			return list;
		}

		private static void AddTagRun(ICollection<Inline> inlines, ref Run run)
		{
			var trimmed = run.Text.Trim('[', '(', '{', ']', ')', '}').ToLowerInvariant();
			if (LocalDatabase.FavoriteTags.AsEnumerable().Any(ft => ft.Tag.ToLowerInvariant() == trimmed)) run.Foreground = Brushes.Red;
			inlines.Add(run);
			run = new Run();
		}

		private static readonly Random Random = new Random();

		public static T SelectRandom<T>(this IList<T> collection)
		{
			if (collection == null || collection.Count == 0) return default;
			return collection[Random.Next(0, collection.Count)];
		}
	}
}

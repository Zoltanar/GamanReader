using System.Diagnostics;
using System.IO;
using System.Reflection;
using GamanReader.Model.Database;
using GamanReader.ViewModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SevenZip;
using static GamanReader.Model.StaticHelpers;

namespace GamanReaderTests
{
	[TestClass]
	public class ContainerFormatTests
	{
		private readonly MainViewModel _viewModel = new MainViewModel();

		public ContainerFormatTests()
		{
			var firstPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			Debug.Assert(firstPath != null, nameof(firstPath) + " != null");
			string path = Path.Combine(firstPath, "7z.dll");
			if (!File.Exists(path))
			{
				LogToFile("Failed to find 7z.dll in same folder as executable.");
				throw new FileNotFoundException("Failed to find 7z.dll in same folder as executable.", path);
			}
			//TODO try default 7zip install folder 
			SevenZipBase.SetLibraryPath(path);
		}

		[TestMethod]
		public void OpenFolder()
		{
			var item = MangaInfo.Create("..\\TestContainerFolder");
			_viewModel.LoadContainer(item);
			Assert.AreEqual(7, _viewModel.TotalFiles);
		}

		[TestMethod]
		public void OpenRar()
		{
			var item = MangaInfo.Create("..\\TestContainerFolder.rar");
			_viewModel.LoadContainer(item);
			Assert.AreEqual(7, _viewModel.TotalFiles);
		}

		[TestMethod]
		public void OpenZip()
		{
			var item = MangaInfo.Create("..\\TestContainerFolder.zip");
			_viewModel.LoadContainer(item);
			Assert.AreEqual(7, _viewModel.TotalFiles);
		}
	}
}

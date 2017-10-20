using System.Diagnostics;
using System.IO;
using System.Reflection;
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

		/*[TestMethod]
		public void OpenFolder()
		{
			_viewModel.LoadFolder("..\\TestContainerFolder");
			Assert.AreEqual(7, _viewModel.TotalFiles);
		}

		[TestMethod]
		public void OpenRar()
		{
			_viewModel.LoadArchive("..\\TestContainerFolder.rar");
			Assert.AreEqual(7, _viewModel.TotalFiles);
		}

		[TestMethod]
		public void OpenZip()
		{
			_viewModel.LoadArchive("..\\TestContainerFolder.zip");
			Assert.AreEqual(7, _viewModel.TotalFiles);
		}*/
	}
}

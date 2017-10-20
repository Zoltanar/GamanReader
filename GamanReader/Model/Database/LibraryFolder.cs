namespace GamanReader.Model.Database
{
	public class LibraryFolder
	{
		public LibraryFolder(string folderPath)
		{
			Path = folderPath;
		}

		public LibraryFolder() { }
		
		public int Id { get; set; }
		public string Path { get; set; }
	}
}
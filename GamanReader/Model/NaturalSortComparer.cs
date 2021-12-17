using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace GamanReader.Model
{
	public class NaturalSortComparer : IComparer<string>
	{
		[DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		public static extern int StrCmpLogicalW(string x, string y);

		public int Compare(string x, string y)
		{
			return StrCmpLogicalW(x, y);
		}
	}
}

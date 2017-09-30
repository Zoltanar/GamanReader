using System;
using System.Collections.Generic;
using System.IO;

namespace GamanReader.ViewModel
{
	internal abstract class ContainerViewModel
	{
		private readonly MainViewModel _mainModel;

		protected ContainerViewModel(MainViewModel mainModel)
		{
			_mainModel = mainModel;
		}

		public string ContainerPath { get; protected set; }
		public int CurrentIndex { get; set; }
		public int TotalFiles { get; protected set; }
		protected string[] FileNames { get; set; }
		public bool HasPreviousPage => CurrentIndex - _mainModel.PageSize > 0;
		public bool HasNextPage => CurrentIndex + _mainModel.PageSize < TotalFiles - 1;
		public bool HasNext => CurrentIndex + 1 < TotalFiles - 1;
		public abstract string GetFile(int index);
		public abstract bool IsFolder { get; }

		protected readonly List<string> RecognizedExtensions = new List<string>();
		protected bool FileIsImage(string filename)
		{
			var ext = Path.GetExtension(filename);
			return RecognizedExtensions.Exists(x =>
			{
				var extension = Path.GetExtension(x);
				return extension != null && extension.Equals(ext);
			});
		}


		public void GoBack(int moveNumber)
		{
			if (CurrentIndex == -1) return; //already at beginning
			CurrentIndex = Math.Max(-1, CurrentIndex - moveNumber);
			PopulateBoxes();
		}

		public void GoForward(int moveNumber)
		{
			if (CurrentIndex == TotalFiles-1) return; //already at end
			CurrentIndex = Math.Min(TotalFiles-1, CurrentIndex + moveNumber);
			PopulateBoxes();
		}

		public void PopulateBoxes()
		{
			ImageBox imagebox1;
			if (_mainModel.PageSize == 1) imagebox1 = ImageBox.Single;
			else imagebox1 = _mainModel.RtlIsChecked ? ImageBox.Right : ImageBox.Left;
			_mainModel.PopulateBox(imagebox1, CurrentIndex);
			if (_mainModel.PageSize == 1) return;
			var imagebox2 = _mainModel.RtlIsChecked ? ImageBox.Left : ImageBox.Right;
			_mainModel.PopulateBox(imagebox2, CurrentIndex+1 > TotalFiles-1 ? -1 : CurrentIndex + 1);
			_mainModel.GoToIndexText = CurrentIndex.ToString();
			_mainModel.IndexLabelText = $"/{TotalFiles}";
		}

		/// <summary>
		/// Method to be called after object is created.
		/// </summary>
		internal void Initialize()
		{
			PopulateBoxes();
		}
	}

	public enum ImageBox
	{
		Single = 0,
		Left = 1,
		Right = 2
	}
}

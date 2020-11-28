using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GamanReader.Model;
using GamanReader.Model.Database;

namespace GamanReader
{
	/// <summary>
	/// Interaction logic for AliasWindow.xaml
	/// </summary>
	public partial class AliasWindow : Window
	{
		public AliasWindow()
		{
			InitializeComponent();
		}

		private void ShowTagsOnEnterKey(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			var comparer = new CaseInsensitiveTagComparer();
			var autoTags = StaticHelpers.LocalDatabase.AutoTags.Where(x => x.Tag.ToLower().Contains(SearchTb.Text.ToLower())).ToArray();
			var userTags = StaticHelpers.LocalDatabase.UserTags.Where(x => x.Tag.ToLower().Contains(SearchTb.Text.ToLower())).ToArray();
			TagsLb.ItemsSource = autoTags.Concat<IndividualTag>(userTags).Distinct(comparer).ToArray();
		}

		private void OnItemClicked(object sender, MouseButtonEventArgs e)
		{
			if (!(e.OriginalSource is DependencyObject source)) return;
			if (!(ItemsControl.ContainerFromElement((ItemsControl)sender, source) is ListBoxItem lbItem)) return;
			if (!(lbItem.Content is IndividualTag item)) return;
			AliasLb.Items.Add(item);

		}


		private void Save(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(AliasNameTb.Text)) return; //todo say it needs name
			var alias = StaticHelpers.LocalDatabase.GetOrCreateAlias(AliasNameTb.Text);
			alias.AddTags(AliasLb.Items.Cast<IndividualTag>());
			Close();
		}

		private void Cancel(object sender, RoutedEventArgs e)
		{
			Close();
		}
		private class CaseInsensitiveTagComparer : IEqualityComparer<IndividualTag>
		{
			public bool Equals(IndividualTag x, IndividualTag y)
			{
				if (x == null && y == null) return true;
				if (x == null ^ y == null) return false;
				return x.Tag.ToLower().Equals(y.Tag.ToLower());
			}

			public int GetHashCode(IndividualTag obj)
			{
				return obj.Tag.ToLower().GetHashCode();
			}
		}
	}
}

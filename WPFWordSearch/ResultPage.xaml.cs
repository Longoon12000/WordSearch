using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WordSearch.Search;

namespace WPFWordSearch
{
    /// <summary>
    /// Interaction logic for ResultPage.xaml
    /// </summary>
    public partial class ResultPage : Page
    {
        private IEnumerable<SearchResult> searchResults;

        public ResultPage(IEnumerable<SearchResult> searchResults)
        {
            InitializeComponent();

            this.searchResults = searchResults;
        }

        private void lvResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is ListView listView))
                return;

            if (!(listView.SelectedItem is SearchResult searchResult))
                return;

            if (!File.Exists(searchResult.FilePath))
            {
                MessageBox.Show($"File not found:{Environment.NewLine}{searchResult.FilePath}", "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Process.Start(searchResult.FilePath);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (searchResults == null || !searchResults.Any())
            {
                MessageBox.Show("No matches were found.", "No results", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.NavigationService.GoBack();
            }

            this.lvResults.ItemsSource = searchResults;
        }
    }
}

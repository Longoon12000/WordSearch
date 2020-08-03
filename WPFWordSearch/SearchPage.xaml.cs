using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WordSearch.Search;

namespace WPFWordSearch
{
    /// <summary>
    /// Interaction logic for SearchPage.xaml
    /// </summary>
    public partial class SearchPage : Page
    {
        private OpenFileDialog openFileDialog = default;

        public SearchPage()
        {
            InitializeComponent();
            this.txtResultWidth.Text = "80";
        }

        private void search(string path, int resultWidth)
        {
            SearchProgress searchProgress;
            using (Search search = new Search(resultWidth))
            {
                searchProgress = new SearchProgress(search, path);
                searchProgress.ShowDialog();
            }

            this.NavigationService.Navigate(new ResultPage(searchProgress.SearchResults));
        }
        private Regex numberValidation = new Regex(@"^\d+$", RegexOptions.Compiled);
        private void txtResultWidth_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !this.numberValidation.IsMatch(e.Text);
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {

            if (this.openFileDialog == default)
                this.openFileDialog = new OpenFileDialog()
                {
                    Filter = "Word Documents (*.doc;*.docx;*.docm)|*.doc;*.docx;*.docm"
                };

            if (this.openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            this.txtPath.Text = this.openFileDialog.FileName;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string path = txtPath.Text;
            int resultWidth = int.Parse(txtResultWidth.Text);

            if (String.IsNullOrEmpty(path))
            {
                System.Windows.MessageBox.Show("No file was provided. Please enter or select a file to search in.", "Missing search file", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!File.Exists(path))
            {
                System.Windows.MessageBox.Show($"The selected search file{Environment.NewLine}{path}{Environment.NewLine}does not exist! Please enter a valid file.", "Search file not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.search(path, resultWidth);
        }
    }
}

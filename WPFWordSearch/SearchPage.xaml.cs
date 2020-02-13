using Microsoft.Win32;
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
        private FolderBrowserDialog folderBrowserDialog = default;
        private RegistryKey registryKey;

        public SearchPage()
        {
            InitializeComponent();

            registryKey = Registry.CurrentUser.OpenSubKey("Software", true).CreateSubKey("WordSearch", true);
            this.txtPath.Text = registryKey.GetValue("Path", "") as string;
            this.cbRecursive.IsChecked = bool.Parse(registryKey.GetValue("Recursive", "True") as string);
            this.txtExpression.Text = registryKey.GetValue("Expression", "") as string;
            this.cbRegex.IsChecked = bool.Parse(registryKey.GetValue("Regex", "False") as string);
            this.txtResultWidth.Text = registryKey.GetValue("Width", "80") as string;
            this.cbParagraph.IsChecked = bool.Parse(registryKey.GetValue("Paragraph", "False") as string);
        }

        private void search(string path, bool recursive, string expression, bool regex, int resultWidth, bool paragraph)
        {
            SearchProgress searchProgress;
            using (Search search = new Search(paragraph ? -1 : resultWidth))
            {
                searchProgress = new SearchProgress(search, path, recursive, expression, regex);
                searchProgress.ShowDialog();
            }

            this.NavigationService.Navigate(new ResultPage(searchProgress.SearchResults));
        }
        private Regex numberValidation = new Regex(@"^\d+$", RegexOptions.Compiled);
        private void txtResultWidth_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !this.numberValidation.IsMatch(e.Text);
        }

        private void cbParagraph_CheckedChanged(object sender, RoutedEventArgs e)
        {
            this.txtResultWidth.IsEnabled = this.cbParagraph.IsChecked.HasValue ? !this.cbParagraph.IsChecked.Value : true;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {

            if (this.folderBrowserDialog == default)
                this.folderBrowserDialog = new FolderBrowserDialog();

            if (this.folderBrowserDialog.ShowDialog() != DialogResult.OK)
                return;

            this.txtPath.Text = this.folderBrowserDialog.SelectedPath;
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            string path = txtPath.Text;
            bool recursive = cbRecursive.IsChecked ?? false;
            string expression = txtExpression.Text;
            bool regex = cbRegex.IsChecked ?? false;
            int resultWidth = cbParagraph.IsChecked ?? false ? -1 : int.Parse(txtResultWidth.Text);
            bool paragraph = cbParagraph.IsChecked ?? false;

            if (String.IsNullOrEmpty(path))
            {
                System.Windows.MessageBox.Show("No search path was provided. Please enter or select a path to search in.", "Missing search path", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Directory.Exists(path))
            {
                System.Windows.MessageBox.Show($"The selected search path{Environment.NewLine}{path}{Environment.NewLine}does not exist! Please enter a valid path.", "Search path not found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (String.IsNullOrEmpty(expression))
            {
                System.Windows.MessageBox.Show($"No search expression was provided. Please enter an expression to search.", "Missing search expression", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            registryKey.SetValue("Path", path);
            registryKey.SetValue("Recursive", recursive);
            registryKey.SetValue("Expression", expression);
            registryKey.SetValue("Regex", regex);
            registryKey.SetValue("Width", txtResultWidth.Text);
            registryKey.SetValue("Paragraph", paragraph);

            this.search(path, recursive, expression, regex, resultWidth, paragraph);
        }
    }
}

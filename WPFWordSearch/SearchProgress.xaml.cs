using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WordSearch.Search;

namespace WPFWordSearch
{
    /// <summary>
    /// Interaction logic for SearchProgress.xaml
    /// </summary>
    public partial class SearchProgress : Window
    {
        private Search search;
        private string path;

        private CancellationTokenSource tokenSource;

        public SearchResult[] SearchResults { get; private set; }

        public SearchProgress(Search search, string path)
        {
            InitializeComponent();
            this.path = path;
            this.search = search;
            this.search.Process += Search_Process;
        }

        private void Search_Process(string status, int current, int total, double percentage)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this.pbProgress.Maximum = total;
                this.pbProgress.Value = current;
                this.lblProgress.Content = $"{status} - {current}/{total} ({Math.Round(percentage * 100, 1)}%)";
            }));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.tokenSource = new CancellationTokenSource();
            this.search.Token = this.tokenSource.Token;
            await Task.Run(() => this.SearchResults = this.search.GetSearchResults(this.path));
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.tokenSource.Cancel();
        }
    }
}

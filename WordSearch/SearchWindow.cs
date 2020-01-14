using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WordSearch.Search;

namespace WordSearch
{
    public partial class SearchWindow : Form
    {
        private FolderBrowserDialog folderBrowserDialog = default;

        public SearchWindow()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            this.gbSearch.Enabled = false;

            string path = txtPath.Text;
            bool recursive = cbRecursive.Checked;
            string expression = txtExpression.Text;
            bool regex = cbRegex.Checked;
            int resultWidth = (int)nudResultWidth.Value;

            if (String.IsNullOrEmpty(path))
            {
                MessageBox.Show("No search path was provided. Please enter or select a path to search in.", "Missing search path", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(path))
            {
                MessageBox.Show($"The selected search path{Environment.NewLine}{path}{Environment.NewLine}does not exist! Please enter a valid path.", "Search path not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (String.IsNullOrEmpty(expression))
            {
                MessageBox.Show($"No search expression was provided. Please enter an expression to search.", "Missing search expression", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Task.Run(() => runSearch(path, recursive, expression, regex, resultWidth));
        }

        private void runSearch(string path, bool recursive, string expression, bool regex, int resultWidth)
        {
            SearchResult[] searchResults = default;

            using (Search.Search search = new Search.Search(resultWidth))
            {
                search.Process += this.Search_Process;
                searchResults = search.GetSearchResults(WordFileLocator.FindWordFiles(path, recursive), expression, regex);
            }

            this.completeSearch(searchResults);
        }

        private void completeSearch(SearchResult[] searchResults)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => completeSearch(searchResults)));
                return;
            }

            this.gbSearch.Enabled = true;
            this.pbProgress.Value = 0;
            int fileCount = searchResults.Select(r => r.FilePath).Distinct().Count();
            this.lblStatus.Text = $"Completed, found {searchResults.Length} result{(searchResults.Length == 1 ? "" : "s")} in {fileCount} file{(fileCount == 1 ? "" : "s")}";

            this.dgvResults.DataSource = new BindingSource(new BindingList<SearchResult>(searchResults), null);

            this.dgvResults.Columns["Text"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            this.dgvResults.Columns["FilePath"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dgvResults.Columns["Text"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            this.dgvResults.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;

            foreach (DataGridViewColumn dataGridViewColumn in this.dgvResults.Columns)
            {
                int columnWidth = dataGridViewColumn.Width;
                dataGridViewColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridViewColumn.Width = columnWidth;
            }
        }

        private void Search_Process(string status, int current, int total, double percentage)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => this.Search_Process(status, current, total, percentage)));
                return;
            }

            this.pbProgress.Maximum = total;
            this.pbProgress.Value = current;
            this.lblStatus.Text = status;
        }

        private void btnFolderPicker_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog == default)
                this.folderBrowserDialog = new FolderBrowserDialog();

            if (this.folderBrowserDialog.ShowDialog() != DialogResult.OK)
                return;

            this.txtPath.Text = this.folderBrowserDialog.SelectedPath;
        }
    }
}

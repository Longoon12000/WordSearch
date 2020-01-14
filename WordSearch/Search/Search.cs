using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word = Microsoft.Office.Interop.Word;

namespace WordSearch.Search
{
    class Search : IDisposable
    {
        private Word.Application application = default;
        private int resultRadius = 0;

        public delegate void ProcessEvent(string status, int current, int total, double percentage);

        public event ProcessEvent Process;

        public string Status { get; private set; }
        public int Total { get; private set; }
        public int Current { get; private set; }
        public double Percentage => (double)this.Current / (double)this.Total;

        public Search(int resultWidth = 80)
        {
            this.resultRadius = (int)Math.Ceiling(resultWidth / 2.0d);
        }

        private void updateProcess(string status, int current, int total)
        {
            this.Status = status;
            this.Current = current;
            this.Total = total;

            this.Process?.Invoke(this.Status, this.Current, this.Total, this.Percentage);
        }

        public SearchResult[] GetSearchResults(string[] files, string expression, bool regex)
        {
            this.OpenWord();

            this.updateProcess($"Searching through {files.Length} files", 0, files.Length);

            return files.SelectMany(f => this.findInDocument(f, readDocument(f), expression.ToLower(), regex)).ToArray();
        }

        private SearchResult[] findInDocument(string filePath, string documentText, string expression, bool regex)
        {
            this.updateProcess($"Searching through file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            List<SearchResult> searchResults = new List<SearchResult>();
            string searchText = documentText.ToLower();

            // normal search
            int lastPosition = 0;
            while (true)
            {
                lastPosition = searchText.IndexOf(expression, lastPosition + 1);

                if (lastPosition == -1)
                    break;

                int resultStartPosition = Math.Max(lastPosition - this.resultRadius, 0);
                int resultEndPosition = Math.Min(lastPosition + this.resultRadius, documentText.Length - 1);

                searchResults.Add(new SearchResult()
                {
                    FilePath = filePath,
                    Text = documentText.Substring(resultStartPosition, resultEndPosition - resultStartPosition)
                });
            }

            // TODO: regex search

            this.Current++;
            this.updateProcess($"Completed file {this.Current}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            return searchResults.ToArray();
        }

        private string readDocument(string filePath)
        {
            this.updateProcess($"Reading file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            Word.Document document = this.application.Documents.Open(FileName: filePath, ReadOnly: true, Visible: false);

            string documentText = String.Join(Environment.NewLine, document.StoryRanges.OfType<Word.Range>().Select(r => r.Text)).Replace("\v", Environment.NewLine);

            document.Close(SaveChanges: false);

            this.updateProcess($"Read file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            return documentText;
        }

        private void OpenWord()
        {
            if (this.application != null)
                return;

            this.updateProcess("Starting word", 0, 1);

            this.application = new Word.Application();
            this.application.Visible = false;

            this.updateProcess("Word has started", 1, 1);
        }

        private void CloseWord()
        {
            if (this.application == null)
                return;

            this.application.Quit(SaveChanges: false);
            this.application = null;
        }

        public void Dispose()
        {
            this.CloseWord();
        }
    }
}

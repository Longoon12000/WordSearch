using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Word = Microsoft.Office.Interop.Word;

namespace WordSearch.Search
{
    public class Search : IDisposable
    {
        private Word.Application application = default;
        private int resultRadius = 0;
        public CancellationToken Token { get; set; }

        public delegate void ProcessEvent(string status, int current, int total, double percentage);

        public event ProcessEvent Process;

        public string Status { get; private set; }
        public int Total { get; private set; }
        public int Current { get; private set; }
        public double Percentage => (double)this.Current / (double)this.Total;

        public Search(int resultWidth = 80)
        {
            this.resultRadius = resultWidth == -1 ? -1 : (int)Math.Ceiling(resultWidth / 2.0d);
        }

        private void updateProcess(string status, int current, int total)
        {
            this.Status = status;
            this.Current = current;
            this.Total = total;

            this.Process?.Invoke(this.Status, this.Current, this.Total, this.Percentage);
        }

        public SearchResult[] GetSearchResults(string file)
        {
            this.OpenWord();

            this.updateProcess($"Searching through {Path.GetFileNameWithoutExtension(file)}", 0, 1);

            return this.findInDocument(file, readDocument(file));
        }

        private readonly char[] trimmables = new char[] { ' ', '\r', '\n', '\t', '\v' };

        private SearchResult[] findInDocument(string filePath, string documentText)
        {
            if (Token.IsCancellationRequested)
                return new SearchResult[0];

            this.updateProcess($"Searching through file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            List<SearchResult> searchResults = new List<SearchResult>();

            MatchCollection matches = Regex.Matches(documentText, @"\[([0-9]+)\]");
            if (matches.Count > 0)
            {
                int lastMatch = 0;
                foreach (Match match in matches)
                {
                    int currentMatch = int.Parse(match.Groups[1].Value); // No cast validation required as regex guarantees an integer - except overflows!
                    if (currentMatch != lastMatch + 1)
                    {
                        searchResults.Add(new SearchResult()
                        {
                            FilePath = filePath,
                            Match = currentMatch,
                            BeforeMatch = documentText.Substring(Math.Max(match.Index - this.resultRadius, 0), this.resultRadius),
                            AfterMatch = documentText.Substring(match.Index + match.Value.Length, documentText.Length - Math.Min(match.Index + match.Value.Length + this.resultRadius, documentText.Length))
                        });
                    }
                    lastMatch = currentMatch;
                }
            }

            this.updateProcess($"Completed file {Path.GetFileNameWithoutExtension(filePath)}", ++this.Current, this.Total);

            return searchResults.ToArray();
        }

        private readonly string paragraphSeparator = $"{Environment.NewLine}\x0001{Environment.NewLine}";

        private string readDocument(string filePath)
        {
            if (Token.IsCancellationRequested)
                return String.Empty;

            this.updateProcess($"Reading file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            Word.Document document = this.application.Documents.Open(FileName: filePath, ReadOnly: true, Visible: false);

            try
            {
                string documentText = String.Join(paragraphSeparator, document.StoryRanges.OfType<Word.Range>().SelectMany(r => r.Paragraphs.OfType<Word.Paragraph>()).Select(p => p.Range.Text)).Replace("\v", paragraphSeparator);

                document.Close(SaveChanges: false);

                this.updateProcess($"Read file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

                return documentText;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private void OpenWord()
        {
            if (this.application != null)
                return;

            this.updateProcess("Starting word", 0, 1);

            this.application = new Word.Application();
            this.application.Visible = false;

            this.updateProcess("Word has started", 1, 1);

            if (Token.IsCancellationRequested)
                this.CloseWord();
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        public SearchResult[] GetSearchResults(string[] files, string expression, bool regex)
        {
            this.OpenWord();

            this.updateProcess($"Searching through {files.Length} files", 0, files.Length);

            return files.SelectMany(f => this.findInDocument(f, readDocument(f), expression.ToLower(), regex)).ToArray();
        }

        private readonly char[] trimmables = new char[] { ' ', '\r', '\n', '\t', '\v' };

        private SearchResult[] findInDocument(string filePath, string documentText, string expression, bool regex)
        {
            if (Token.IsCancellationRequested)
                return new SearchResult[0];

            this.updateProcess($"Searching through file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            List<SearchResult> searchResults = new List<SearchResult>();
            string searchText = documentText.ToLower();
            string[] splitDocument = documentText.Split(new string[] { paragraphSeparator }, StringSplitOptions.None);
            string[] splitSearch = searchText.Split(new string[] { paragraphSeparator }, StringSplitOptions.None);

            for (int i = 0; i < splitDocument.Length; i++)
            {
                if (Token.IsCancellationRequested)
                    return searchResults.ToArray();

                string currentSearchText = splitSearch[i];
                // normal search
                int lastPosition = -1;
                while (true)
                {
                    if (Token.IsCancellationRequested)
                        return searchResults.ToArray();

                    lastPosition = currentSearchText.IndexOf(expression, lastPosition + 1);

                    if (lastPosition == -1)
                        break;

                    // Try and find start of word
                    int previousSpace = currentSearchText.LastIndexOf(' ', lastPosition);
                    int previousBreak = currentSearchText.LastIndexOf('\r', lastPosition);

                    int positionWordStart = Math.Max(previousSpace, previousBreak);
                    if (positionWordStart == -1)
                        positionWordStart = 0;

                    // Try and find end of word
                    int nextSpace = currentSearchText.IndexOf(' ', lastPosition);
                    int nextBreak = currentSearchText.IndexOf('\r', lastPosition);
                    if (nextSpace == -1) nextSpace = int.MaxValue;
                    if (nextBreak == -1) nextBreak = int.MaxValue;

                    int positionWordEnd = Math.Min(nextSpace, nextBreak);
                    if (positionWordEnd == int.MaxValue)
                        positionWordEnd = currentSearchText.Length;

                    SearchResult result = new SearchResult
                    {
                        FilePath = filePath,
                        Match = splitDocument[i].Substring(positionWordStart, positionWordEnd - positionWordStart).Trim(trimmables)
                    };

                    if (this.resultRadius == -1)
                    {
                        // Whole paragraph
                        result.BeforeMatch = splitDocument[i].Substring(0, positionWordStart).Trim(trimmables);
                        result.AfterMatch = splitDocument[i].Substring(positionWordEnd, currentSearchText.Length - positionWordEnd).Trim(trimmables);
                    }
                    else
                    {
                        // Specific length

                        // before
                        int index = i;
                        int remainder = this.resultRadius;

                        int startIndex = Math.Max(positionWordStart - remainder, 0);
                        result.BeforeMatch = $"{splitDocument[index--].Substring(startIndex, positionWordStart - startIndex)}";
                        remainder -= positionWordStart - startIndex;

                        while (index >= 0 && remainder > 0)
                        {
                            startIndex = Math.Max(splitDocument[index].Length - remainder, 0);
                            remainder -= splitDocument[index].Length - startIndex;
                            result.BeforeMatch = $"{splitDocument[index].Substring(startIndex, splitDocument[index].Length - startIndex).Trim(trimmables)}{Environment.NewLine}{result.BeforeMatch}";
                            index--;
                        }

                        // after
                        index = i;
                        remainder = this.resultRadius;

                        int endIndex = Math.Min(positionWordEnd + remainder, currentSearchText.Length);
                        result.AfterMatch += $"{splitDocument[index++].Substring(positionWordEnd, endIndex - positionWordEnd).Trim(trimmables)}{Environment.NewLine}";
                        remainder -= endIndex - positionWordEnd;

                        while (index < splitDocument.Length && remainder > 0)
                        {
                            endIndex = Math.Min(remainder, splitDocument[index].Length);
                            remainder -= endIndex;
                            result.AfterMatch += $"{splitDocument[index].Substring(0, endIndex).Trim(trimmables)}{Environment.NewLine}";
                            index++;
                        }
                    }

                    result.AfterMatch = result.AfterMatch?.Trim(trimmables);
                    result.BeforeMatch = result.BeforeMatch?.Trim(trimmables);

                    searchResults.Add(result);
                }
            }

            // TODO: regex search

            this.Current++;
            this.updateProcess($"Completed file {this.Current}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            return searchResults.ToArray();
        }

        private readonly string paragraphSeparator = $"{Environment.NewLine}\x0001{Environment.NewLine}";

        private string readDocument(string filePath)
        {
            if (Token.IsCancellationRequested)
                return String.Empty;

            this.updateProcess($"Reading file {this.Current + 1}/{this.Total} ({Path.GetFileNameWithoutExtension(filePath)})", this.Current, this.Total);

            Word.Document document = this.application.Documents.Open(FileName: filePath, ReadOnly: true, Visible: false);

            string documentText = String.Join(paragraphSeparator, document.StoryRanges.OfType<Word.Range>().SelectMany(r => r.Paragraphs.OfType<Word.Paragraph>()).Select(p => p.Range.Text)).Replace("\v", paragraphSeparator);

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

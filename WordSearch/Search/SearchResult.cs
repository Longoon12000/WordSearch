using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordSearch.Search
{
    class SearchResult
    {
        [DisplayName("File Path")]
        public string FilePath { get; set; }
        [DisplayName("Text")]
        public string Text { get; set; }
    }
}

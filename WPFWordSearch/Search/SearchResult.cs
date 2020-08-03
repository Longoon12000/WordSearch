using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordSearch.Search
{
    public class SearchResult
    {
        public string FilePath { get; set; }
        public int Match { get; set; }
        public string BeforeMatch { get; set; }
        public string AfterMatch { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordSearch.Search
{
    static class WordFileLocator
    {
        private static readonly string[] WordExtensions =
        {
            ".doc",
            ".docx",
            ".docm",
            ".dot",
            ".dotx",
            ".dotm",
            ".odt"
        };

        public static string[] FindWordFiles(string basePath, bool recursive)
        {
            List<string> wordFiles = new List<string>();

            wordFiles.AddRange(Directory.GetFiles(basePath, "*").Where(p => WordExtensions.Contains(Path.GetExtension(p).ToLower()) && !Path.GetFileName(p).StartsWith("~$")));

            if (recursive)
                wordFiles.AddRange(Directory.GetDirectories(basePath).SelectMany(d => FindWordFiles(d, recursive)));

            return wordFiles.ToArray();
        }
    }
}

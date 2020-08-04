using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordLinkOrderChecker
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get word file
            Console.WriteLine("Enter Word file path:");
            string path = Console.ReadLine();

            // Initialise WordLinkOrder
            WordLinkOrder.WordLinkOrder wordLinkOrder = null;
            try
            {
                wordLinkOrder = new WordLinkOrder.WordLinkOrder(path);
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"The file {ex.FileName} could not be found.");
                return;
            }

            // Make output verbose (uncomment this line for extra output about the search process)
            //wordLinkOrder.Verbose = true;

            // Execute reference search
            int[] references = wordLinkOrder.Run();

            // Check reference order
            int lastNumber = 0;
            bool hasBadReferences = false;
            foreach (int reference in references)
            {
                if (reference != lastNumber + 1)
                {
                    Console.WriteLine($"Bad reference order found: Got [{reference}] but expected [{lastNumber + 1}]!");
                    hasBadReferences = true;
                }

                lastNumber = reference;
            }

            // Conclusion
            if (hasBadReferences)
                Console.WriteLine("There are references in the wrong order!");
            else
                Console.WriteLine("All references are in the correct order!");

            // Keep window open
            Console.WriteLine($"{Environment.NewLine}Press any key to exit.");
            Console.ReadKey();
        }
    }
}

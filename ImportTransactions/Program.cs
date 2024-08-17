using CommandLine;

namespace Finance;

class Program
{

    public class Options
    {

        [Option('c', "cashbook", Required = true, HelpText = "Set the cash book file name to import into.")]
        public string Cashbook { get; set; } = string.Empty;
        
        [Option('p', "pattern", Required = false, HelpText = "Set the import file patterns (filespec)")]
        public string? ImportPatterns { get; set; }
    }


    /// <summary>
    /// Main program entrypoint.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    static void Main(string[] args)
    {
        try
        {
            Parser.Default.ParseArguments<Options>(args)
                           .WithParsed<Options>(options =>
            {
                // Open the cash book.
                var transactions = Excel.Read<Transaction>(options.Cashbook, "Transactions");

                // Import transactions if there are any.
                if (options.ImportPatterns != null)
                {
                    string? directory = Path.GetDirectoryName(options.ImportPatterns);
                    if (directory == string.Empty || directory == null)
                        directory = Directory.GetCurrentDirectory();
                    foreach (var fileName in Directory.GetFiles(directory, Path.GetFileName(options.ImportPatterns)))
                    {
                        Console.WriteLine($"Importing transactions from {fileName}");
                        var transactionsToImport = BankTransactionFile.Read(fileName);
                        if (transactionsToImport != null)
                            transactions = Transactions.Merge(transactions, transactionsToImport);
                    }
                }

                // Predict missing categories.
                Transactions.PredictCategories(transactions);

                // Sort the transactions.
                transactions = Transactions.Sort(transactions);

                // Write spreadsheet.
                Console.WriteLine($"Write to {options.Cashbook}");
                Excel.Write(options.Cashbook, "Transactions", transactions);
            });
        }
        catch (Exception err)
        {
            Console.WriteLine(err.ToString());
        }
    }
}
using CommandLine;

namespace Finance;

class Program
{

    public class Options
    {
        [Option('p', "pattern", Required = false, HelpText = "Set the import file patterns (filespec)")]
        public string? ImportPatterns { get; set; }

        [Option('c', "cashbook", Required = false, HelpText = "Set the cash book file name to import into.")]
        public string? Cashbook { get; set; }

    }


    static void Main(string[] args)
    {
        /// Main entry point
        try
        {
            Parser.Default.ParseArguments<Options>(args)
                           .WithParsed<Options>(options =>
            {
                if (options.Cashbook == null)
                    throw new Exception("No cashbook specified.");
                if (options.ImportPatterns == null)
                    throw new Exception("No import patterns (filespecs) specified.");

                var transactions = Excel.Read<Transaction>(options.Cashbook, "Transactions");
                var transactionsToImport = BankTransactionFile.Read(options.ImportPatterns);
                if (transactionsToImport != null)
                {
                    var mergedTransactions = Transactions.Merge(transactions, transactionsToImport)
                                                         .OrderBy(t => t.Date);
                    Transactions.PredictCategories(mergedTransactions);
                    Excel.Write(options.Cashbook, "Transactions", mergedTransactions);
                }
            });
        }
        catch (Exception err)
        {
            Console.WriteLine(err.ToString());
        }
    }
}
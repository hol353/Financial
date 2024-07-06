using System.Text.RegularExpressions;

namespace Finance;

/// <summary>
/// Encapsulates a file, containing transactions, that has been exported from a bank. Typically a .csv file.
/// </summary>
public class BankTransactionFile
{
    /// <summary>
    /// Merge transactions.
    /// </summary>
    /// <param name="fileNames"></param>
    public static IEnumerable<Transaction> Read(IEnumerable<string> fileNames)
    {
        // Some fileNames may be patterns (filespecs). If so expand them to filenames
        List<string> expandedFileNames = [];
        foreach (string f in fileNames)
        {
            var dir = Path.GetDirectoryName(f);
            if (dir != null)
                expandedFileNames.AddRange(Directory.GetFiles(dir, Path.GetFileName(f)));
        }

        IEnumerable<Transaction> transactionsToImport = [];
        foreach (var fileName in expandedFileNames)
        {
            var transactions = Read(fileName);
            if (transactions != null)
                transactionsToImport = transactionsToImport.Concat(transactions);
        }
        return transactionsToImport.OrderBy(transation => transation.Date);
    }

    /// <summary>
    /// Read transactions from a bank file.
    /// </summary>
    /// <param name="fileName">The file name to read.</param>
    /// <returns>A DataTable with all transactions.</returns>
    public static IEnumerable<Transaction>? Read(string path)
    {
        List<Transaction>? allTransactions = new();

        string fullPath = Path.GetFullPath(path, Directory.GetCurrentDirectory());

        #pragma warning disable CS8604 // Possible null reference argument.
        foreach (var fileName in Directory.GetFiles(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath)))
        {
            IEnumerable<Transaction>? accountTransactions;
            var match = Regex.Match(fileName, @"\d+_\d+_(\d+)_([A-Z]\d+)_[\w\d]+\.csv+");
            if (match.Success)
            {
                string accountName = match.Groups[2].ToString();

                // Heritage Bank.
                // Skip the first two lines. These are current balance lines.
                IEnumerable<string> lines = File.ReadAllLines(fileName);
                lines = lines.Skip(2);
                string headerLine = lines.First() + ", Account";
                var dataLines = lines.Skip(1).Select(line => line + "," + accountName);
                string tempFileName = Path.GetTempFileName();
                File.WriteAllLines(tempFileName, dataLines.Prepend(headerLine));

                // Column mappings.
                var columnMap = new Dictionary<string, string> 
                {  
                    { "Date", "Transaction Date" }
                };

                accountTransactions = Csv.Read<Transaction>(tempFileName, columnMap);
                File.Delete(tempFileName);
            }
            else
            {
                // Column mappings.
                var columnMap = new Dictionary<string, string> 
                {  
                    { "Account", "Account Number" },
                    { "Reference", "Transaction Details" }
                };            
                accountTransactions = Csv.Read<Transaction>(fileName, columnMap);
            }

            #pragma warning restore CS8604 // Possible null reference argument.
            if (accountTransactions != null)
            {
                // Empty the category property. Some banks supply a category but we don't want to use it.
                // Use our category predicted instead.
                foreach (var transaction in accountTransactions)
                    transaction.Category = string.Empty;

                // DataTable needs to be sorted from lowest to highest date. Reverse order if needed.
                if (accountTransactions.First().Date > accountTransactions.Last().Date)
                    accountTransactions = accountTransactions.Reverse();

                allTransactions.AddRange(accountTransactions);            
            }
        }

        // Convert the DataTable into a list of transactions.
        return allTransactions;
    }
}
using System.Text.RegularExpressions;

namespace ImportTransactions;

/// <summary>
/// Encapsulates a file, containing transactions, that has been exported from a bank. Typically a .csv file.
/// </summary>
public class BankTransactionFile
{
    /// <summary>
    /// Read transactions from a bank file.
    /// </summary>
    /// <param name="fileName">The file name to read.</param>
    /// <returns>A DataTable with all transactions.</returns>
    public static IEnumerable<Transaction> Read(string fileName)
    {
        IEnumerable<Transaction> transactions;

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

            transactions = Csv.Read<Transaction>(tempFileName, columnMap);
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
            transactions = Csv.Read<Transaction>(fileName, columnMap);
        }

        // DataTable needs to be sorted from lowest to highest date. Reverse order if needed.
        if (transactions.First().Date > transactions.Last().Date)
            transactions = transactions.Reverse();

        // Convert the DataTable into a list of transactions.
        return transactions;
    }
}
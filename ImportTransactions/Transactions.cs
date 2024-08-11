using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.ML;
using Microsoft.ML.Data;
using MoreLinq;
using Tensorflow;

namespace Finance;

/// <summary>
/// Methods that work on collections of transactions.
/// </summary>
public class Transactions
{
    /// <summary>
    /// Merge 2 collections of transactions
    /// </summary>
    /// <param name="existing">The existing transactions collection.</param>
    /// <param name="newTransactions">The newly imported transactions to merge into existing.</param>
    /// <returns>A merged collection.</returns>
    public static IEnumerable<Transaction> Merge(IEnumerable<Transaction> existing1, IEnumerable<Transaction> newTransactions)
    {
        var existingForAccount = existing1.Where(t => t.Account == newTransactions.First().Account);
        var existingOtherAccounts = existing1.Where(t => t.Account != newTransactions.First().Account);

        // Because bank transactions can be reordered by the bank and as a result balances can change, need to find
        // the first new transaction that exists in the existing transaction collection.
        
        Transaction? firstValidNewTransaction = null;
        foreach (var newTransaction in newTransactions)
        {
            var matchedExisting = existingForAccount.FirstOrDefault(t => t.Date == newTransaction.Date &&
                                                                    t.Account == newTransaction.Account &&
                                                                    t.Amount == newTransaction.Amount &&
                                                                    t.Balance == newTransaction.Balance);
            if (matchedExisting != null)
            {
                firstValidNewTransaction = newTransaction;
                break;
            }
        }

        if (firstValidNewTransaction == null)
            throw new Exception("Cannot import new transactions. There needs to be more overlap of existing and new transactions");

        // Skip new transactions before our first valid transaction.
        newTransactions = newTransactions.SkipWhile(t => t != firstValidNewTransaction);

        // Find first and last transaction date.
        DateTime firstDate = newTransactions.Min(t => t.Date);
        DateTime lastDate = newTransactions.Max(t => t.Date);

        // Find existing transactions in the date range.
        var existingToRemove = existingForAccount.Where(t => t.Date >= firstDate && t.Date <= lastDate);

        // Try and give each new transaction a category from the matching existing transaction.
        foreach (var importedTransaction in newTransactions)
        {
            // Find matching transaction in existing, allowing for date and reference change.
            var foundTransaction = existingToRemove.FirstOrDefault(t => t.CloseMatch(importedTransaction));
            if (foundTransaction != null)
            {
                // Update category
                importedTransaction.Category = foundTransaction.Category;
                importedTransaction.Details = foundTransaction.Details;
                importedTransaction.InvoiceReceipt = foundTransaction.InvoiceReceipt;
            }
            else
                importedTransaction.Details = "??????";
        }

        // Return a sorted merged list.
        var existingToKeep = existingForAccount.Except(existingToRemove);
        return Sort(existingToKeep.Concat(newTransactions)
                                  .Concat(existingOtherAccounts));
    }

    /// <summary>
    /// Sort transactions so that they are in order and their balances are correct.
    /// Some banks transactions are out-of-order for a given day.
    /// </summary>
    /// <param name="transactions">The transactions to sort.</param>
    public static IEnumerable<Transaction> Sort(IEnumerable<Transaction> transactions)
    {
        List<Transaction> sortedTransactions = new();
        
        foreach (var account in transactions.Select(t => t.Account)
                                            .Distinct()
                                            .Order())
        {
            var accountTransactions = transactions.Where(t => t.Account == account)
                                                  .OrderBy(t => t.Date)
                                                  .ToList();

            double runningBalance = FindStartingBalance(accountTransactions);
            
            // Find the first transaction that matches the running balance.
            int numTransactionsSorted = 0;
            Transaction? match;
            while ((match = accountTransactions.Find(t => Math.Round(t.Balance - t.Amount, 2) == runningBalance)) != null)
            {
                // Move transaction to sortedTransactions.
                sortedTransactions.Add(match);
                accountTransactions.Remove(match);

                runningBalance += match.Amount;
                runningBalance = Math.Round(runningBalance, 2);
                numTransactionsSorted++;
            }

            if (numTransactionsSorted < accountTransactions.Count())
                throw new Exception("Some transations not sorting - aborting.");
        }

        return sortedTransactions.OrderBy(t => t.Date).ThenBy(t => t.Account);  
    }

    /// <summary>
    /// Find a starting balance
    /// </summary>
    /// <param name="accountTransactions">Account transactions.</param>
    /// <returns>The starting balance.</returns>
    private static double FindStartingBalance(IEnumerable<Transaction> accountTransactions)
    {
        // Find the lowest date.
        var lowestDate = accountTransactions.Min(t => t.Date);

        var transactionsForFirstDate = accountTransactions.Where(t => t.Date == lowestDate);
        foreach (var transaction in transactionsForFirstDate)
        {
            double previousBalance = transaction.Balance - transaction.Amount;
            
            // If the previousBalance doesn't match a balance for transaction for this date then
            // that will be the starting date.
            var matchedTransactions = transactionsForFirstDate.Where(t => t.Balance == previousBalance);
            if (!matchedTransactions.Any())
                return previousBalance;
        }
        throw new Exception("Cannot find a first transaction to start running balance");        
    }

    /// <summary>
    /// Use ML to predict categories for bank transactions.
    /// </summary>
    /// <remarks>
    /// https://github.com/jernejk/MLSample.SimpleTransactionTagging
    /// </remarks>
    /// <param name="transactions">The collection of transaction instances.</param>
    public static void PredictCategories(IEnumerable<Transaction> transactions)
    {
        var transactionsWithCategories = transactions.Where(t => !string.IsNullOrEmpty(t.Category));

        var mlContext = new MLContext(0);
        var trainingService = new BankTransactionTrainingService(mlContext);
        var mlModel = trainingService.ManualTrain(transactionsWithCategories);

        var predictionEngine = mlContext.Model.CreatePredictionEngine<Transaction, TransactionPrediction>(mlModel);

        var categories = GetCategories(predictionEngine);

        foreach (var transaction in transactions.Where(t => string.IsNullOrEmpty(t.Category)))
        {
            var prediction =predictionEngine.Predict(transaction);
            if (prediction != null && prediction.Category != null && prediction.Score != null)
            {
                var index = categories.IndexOf(prediction.Category);
                if (prediction.Score[index] > 0.5)
                {
                    transaction.Category = prediction.Category;
                    Console.WriteLine($"Ref: {transaction.Reference}. Predicted category: {prediction.Category}. Score: {prediction.Score[index]}");
                }
            }
        }
    }

    private static List<string> GetCategories(PredictionEngine<Transaction, TransactionPrediction> predictionEngine)
    {
        // Based on https://github.com/dotnet/docs/issues/14265
            
        var schema = predictionEngine.OutputSchema;
        var column = schema.GetColumnOrNull("Score");
        if (column == null)
            throw new Exception("Cannot find Score column");

        var slotNames = new VBuffer<ReadOnlyMemory<char>>();
        column.Value.GetSlotNames(ref slotNames);
        var names = new string[slotNames.Length];

        return slotNames
            .DenseValues()
            .Select(x => x.ToString())
            .ToList();
    }

    private class TransactionPrediction
    {
        [ColumnName("PredictedLabel")]
        public string? Category { get; set; }

        public float[]? Score { get; set; }
    }    
}
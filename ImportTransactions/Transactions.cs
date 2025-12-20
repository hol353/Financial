
using Microsoft.ML;
using Microsoft.ML.Data;
using MoreLinq.Extensions;

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
        {
            Console.WriteLine($"!!!! No new transactions found in account {newTransactions.First().Account}");
            return existing1;
        }

        // Skip new transactions before our first valid transaction.
        newTransactions = newTransactions.SkipWhile(t => t != firstValidNewTransaction);

        // Find first and last transaction date.
        DateTime firstDate = newTransactions.Min(t => t.Date);
        DateTime lastDate = newTransactions.Max(t => t.Date);

        // Find existing transactions in the date range.
        var splitTransactions = existingForAccount.Where(t => t.Split != string.Empty);
        var nonSplitTransactions = existingForAccount.Except(splitTransactions).ToList();
        var existingToRemove = nonSplitTransactions.Where(t => t.Date >= firstDate && t.Date <= lastDate);

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
        var existingToKeep = nonSplitTransactions.Except(existingToRemove)
                                                 .Concat(splitTransactions);
        return Sort2(existingToKeep.Concat(newTransactions)
                                   .Concat(existingOtherAccounts));
    }

    /// <summary>
    /// Sort transactions so that they are in order and their balances are correct.
    /// Some banks transactions are out-of-order for a given day.
    /// </summary>
    /// <param name="transactions">The transactions to sort.</param>
    public static IEnumerable<Transaction> Sort2(IEnumerable<Transaction> transactions)
    {
        List<Transaction> sortedTransactions = new();

        var splitTransactions = transactions.Where(t => t.Split != string.Empty);
        var nonSplitTransactions = transactions.Except(splitTransactions).ToList();

        List<Transaction> unmatchedTransactions = new();
        foreach (var account in nonSplitTransactions.Select(t => t.Account)
                                                    .Distinct()
                                                    .Order())
        {
            var accountTransactions = nonSplitTransactions.Where(t => t.Account == account)
                                                          .OrderBy(t => t.Date)
                                                          .ToList();
            List<Transaction> accountTransactionsSorted = new();
            foreach (var transaction in accountTransactions)
            {
                var previousTransaction = accountTransactions.Find(t => t.Balance == transaction.PreviousBalance);

                int index = previousTransaction == null ? -1 : accountTransactionsSorted.FindIndex(t => t.Equals(previousTransaction));

                if (previousTransaction == null || index == -1)
                    accountTransactionsSorted.Add(transaction);
                else
                    accountTransactionsSorted.Insert(index + 1, transaction);
            }
            sortedTransactions.AddRange(accountTransactionsSorted);
        }
        return sortedTransactions.Concat(splitTransactions)
                                 .OrderBy(t => t.Date).ThenBy(t => t.Account);  
    }    

    /// <summary>
    /// Sort transactions so that they are in order and their balances are correct.
    /// Some banks transactions are out-of-order for a given day.
    /// </summary>
    /// <param name="transactions">The transactions to sort.</param>
    public static IEnumerable<Transaction> Sort(IEnumerable<Transaction> transactions)
    {
        List<Transaction> sortedTransactions = new();

        var splitTransactions = transactions.Where(t => t.Split != string.Empty);
        var nonSplitTransactions = transactions.Except(splitTransactions).ToList();

        foreach (var account in nonSplitTransactions.Select(t => t.Account)
                                                    .Distinct()
                                                    .Order())
        {
            var accountTransactions = nonSplitTransactions.Where(t => t.Account == account)
                                                          .OrderBy(t => t.Date)
                                                          .ToList();

            //var firstTransaction = FindStartingTransaction(accountTransactions);
            var runningBalance = double.MinValue; //firstTransaction.Balance - firstTransaction.Amount;
            var transactionDates = accountTransactions.Select(t => t.Date).Distinct().ToArray();            

            foreach (var date in transactionDates)
            {
                if (date == new DateTime(2025, 11, 28))
                {
                    
                }
                List<Transaction> transactionsForDate = accountTransactions.FindAll(t => t.Date == date);

                // Put transactions for this date into a consistent, sorted state.
                int iteration = 0;
                while (!IsSorted(transactionsForDate))
                {
                    for (int i = 0; i != transactionsForDate.Count; i++)
                    {
                        var transaction = transactionsForDate[i];
                        int toIndex;
                        var previousTransaction = transactionsForDate.Find(t => t.Balance == transaction.PreviousBalance);
                        if (previousTransaction == null || transaction.PreviousBalance == runningBalance)
                        {
                            // Must be first item.
                            toIndex = 0;
                        }
                        else
                        {
                            toIndex = transactionsForDate.FindIndex(t => t.Equals(previousTransaction));
                            if (toIndex == -1)
                                throw new Exception($"Cannot find previous transaction index. Aborting...");
                            toIndex++;
                        }

                        int currentIndex =  transactionsForDate.FindIndex(t => t.Equals(transaction));

                        // Move the transaction if necessary
                        if (toIndex != currentIndex)
                            transactionsForDate = transactionsForDate.Move(currentIndex, 1, toIndex).ToList();
                    }

                iteration++;
                if (iteration > transactionsForDate.Count())
                    throw new Exception($"Cannot sort transactions for account {account}");

                }

                // Make sure the first transaction balance matches the previous last transaction in sortedTransactions
                if (sortedTransactions.Count > 0 && transactionsForDate.First().PreviousBalance != sortedTransactions.Last().Balance)
                    throw new Exception($"Discontinuity found while sorting. There is a mistach in balances between {sortedTransactions.Last().Date} and {transactionsForDate.First().Date}");

                // Order the transactions for this date so that the balances for each transaction work.
                sortedTransactions.AddRange(transactionsForDate);
                runningBalance = sortedTransactions.Last().Balance;
                runningBalance = Math.Round(runningBalance, 2);
            }
        }

        return sortedTransactions.Concat(splitTransactions)
                                 .OrderBy(t => t.Date).ThenBy(t => t.Account);  
    }

    private static bool IsSorted(IList<Transaction> transactions)
    {
        for (int i = 1; i < transactions.Count; i++)
        {
            if (transactions[i].PreviousBalance != transactions[i-1].Balance)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Order the transactions for this date so that the balances for each transaction work.
    /// </summary>
    /// <param name="transactions"></param>
    /// <param name="runningBalance"></param>
    /// <returns></returns>
    private static IEnumerable<Transaction> SortTransactions(IEnumerable<Transaction> transactions, double runningBalance)
    {
        foreach (var transaction in transactions)
        {
            if (Math.Round(transaction.Balance - transaction.Amount, 2) == Math.Round(runningBalance, 2))
            {
                var otherTransactions = transactions.Where(t => t != transaction);
                if (!otherTransactions.Any())
                    return [ transaction ];
                else
                {
                    var otherTransactionsSorted = SortTransactions(otherTransactions, runningBalance + transaction.Amount);
                    if (otherTransactionsSorted != null)
                        return new Transaction[] { transaction }.Concat(otherTransactions);
                }
            }
        }
        throw new Exception($"Cannot sort transactions for date {transactions.First().Date}");
    }

    /// <summary>
    /// Shift an array to the left.
    /// </summary>
    /// <param name="transactions"></param>
    /// <param name="numToShift"></param>
    /// <returns></returns>
    public static IEnumerable<Transaction> ShiftLeft(IEnumerable<Transaction> transactions, int numToShift) 
    {
        return transactions.Skip(numToShift).Concat(transactions.Take(numToShift));
    }    

    /// <summary>
    /// Find a starting balance
    /// </summary>
    /// <param name="accountTransactions">Account transactions.</param>
    /// <returns>The starting balance.</returns>
    private static Transaction FindStartingTransaction(IEnumerable<Transaction> accountTransactions)
    {
        // Find the lowest date.
        var lowestDate = accountTransactions.Min(t => t.Date);

        var transactionsForFirstDate = accountTransactions.Where(t => t.Date == lowestDate);
        double previousBalance = 0;
        foreach (var transaction in transactionsForFirstDate)
        {
            previousBalance = transaction.Balance - transaction.Amount;
            
            // If the previousBalance doesn't match a balance for transaction for this date then
            // that will be the starting date.
            var matchedTransactions = transactionsForFirstDate.Where(t => t.Balance == Math.Round(previousBalance, 2));
            if (!matchedTransactions.Any())
                return transaction;
        }
        if (transactionsForFirstDate.Any())
            return transactionsForFirstDate.First();

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
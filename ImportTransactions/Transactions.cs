
using Microsoft.ML;
using Microsoft.ML.Data;
using MoreLinq.Extensions;

namespace Finance;

/// <summary>
/// Methods that work on collections of transactions.
/// </summary>
public class Transactions
{
    private IEnumerable<Transaction> transactions;

    /// <summary>
    /// Create a new instance of the Transactions class.
    /// </summary>
    /// <param name="transactions"></param>
    public Transactions(IEnumerable<Transaction> transactions)
    {
        this.transactions = Sort(transactions).ToArray();
    }

    /// <summary>
    /// Get all transactions.
    /// </summary>
    public IEnumerable<Transaction> All => transactions;

    /// <summary>
    /// Import a set of transactions into the current set of transactions for an account.
    /// </summary>
    /// <param name="account">Account name.</param>
    public void Import(IEnumerable<Transaction> newTransactions)
    {
        var account = newTransactions.First().Account;
        var accountTransactions = TransactionsWithoutSplits(account);

        // Remove transactions starting from the first date in 'newTransactions'
        var dateOfFirstNewTransaction = newTransactions.First().Date;
        var transactionsToKeep = accountTransactions.Where(t => t.Date < dateOfFirstNewTransaction);
        var transactionsToDiscard = accountTransactions.Where(t => t.Date >= dateOfFirstNewTransaction);
      
        // Copy categories from discard pile to keep pile.
        CopyCategories(transactionsToDiscard, newTransactions);

        // Ensure the balance is correct. If not, then the import is invalid.
        if (transactionsToKeep.Any())
        {
            var lastTransactionToKeep = transactionsToKeep.Last();
            var firstNewTransaction = newTransactions.First();
            if (lastTransactionToKeep.Balance + firstNewTransaction.Amount != firstNewTransaction.Balance)
                throw new Exception($"Balance mismatch: {lastTransactionToKeep.Date} != {firstNewTransaction.Date}");
        }

        // Set the new transactions for the account.
        SetTransactionsWithoutSplits(account, transactionsToKeep.Concat(newTransactions));
    }

    /// <summary>
    /// Sort transactions so that they are in order date, then account and their balances are consistent.
    /// </summary>
    public static IEnumerable<Transaction> Sort(IEnumerable<Transaction> transactions)
    {
        List<Transaction> sortedTransactions = new();

        foreach (var account in transactions.Select(t => t.Account)
                                            .Distinct())
        {
            var accountTransactions = transactions.Where(t => t.Account == account);
            double currentBalance = double.MinValue;

            foreach (var day in accountTransactions
                .GroupBy(t => t.Date.Date)
                .OrderBy(g => g.Key))
            {
                // Sort all transactions that are NOT a split transaction.
                var remaining = day.Where(t => string.IsNullOrEmpty(t.Split)).ToList();
                var sortedDay = new List<Transaction>(remaining.Count);

                if (currentBalance == double.MinValue)
                {
                    // Find the transaction that can start the sequence.
                    var first = FindFirstTransaction(remaining);
                    remaining.Remove(first);
                    sortedDay.Add(first);
                    currentBalance = first.Balance;
                }

                if (!TrySortTransactions(remaining, currentBalance, sortedDay))
                {
                    // No consistent next transaction was found. Stop rather than silently inventing an order.
                    throw new InvalidOperationException(
                        $"Cannot reconstruct transaction sequence for {day.Key:yyyy-MM-dd}. " +
                        $"Expected a transaction starting at balance {currentBalance:F2}.");
                }

                // Add sorted transactions for the day.
                foreach (var transaction in sortedDay)
                {
                    sortedTransactions.Add(transaction);
                    currentBalance = transaction.Balance;
                }

                // Now add any splits at the end of the day.
                sortedTransactions.AddRange(day.Where(t => !string.IsNullOrEmpty(t.Split)));
            }
        }
        // Order the transactions by Date then account.
        return sortedTransactions.OrderBy(t => t.Date).ThenBy(t => t.Account);
    }

    /// <summary>
    /// Copy transactions from one collection to another. Match transactions
    /// on date/amount/description.
    /// </summary>
    /// <param name="from">From collection.</param>
    /// <param name="to">To collection.</param>
    private static void CopyCategories(IEnumerable<Transaction> from, IEnumerable<Transaction> to)
    {
        foreach (var t in to)
        {
            var match = from.FirstOrDefault(f => CloseMatch(f, t));
            if (match == null)
                t.Details = "??????";
            else
                t.Category = match.Category;
        }        
    }

    /// <summary>
    /// Tests equality between this transaction and another  allowing dates and references to change slightly.
    /// Some banks change the date and references of their transactions.
    /// </summary>
    /// <param name="other">The other transaction.</param>
    /// <param name="exactDate">Dates must match exactly?</param>
    /// <returns>True if equal</returns>
    private static bool CloseMatch(Transaction first, Transaction other)
    {
        if (first.Account == other.Account && first.Amount == other.Amount && 
            (other.Date - first.Date).Days < 10)
        {
            string[] tokens = first.Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] otherTokens = other.Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double percent = tokens.Count(t => otherTokens.Contains(t)) * 1.0 / tokens.Length * 100;
            return percent >= 50;
        }
        return false;
    }    

    /// <summary>
    /// Get all transactions for an account that are not split transactions.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    private IEnumerable<Transaction> TransactionsWithoutSplits(string account) 
        => transactions.Where(t => t.Account == account && string.IsNullOrEmpty(t.Split));

    /// <summary>
    /// Set the transactions for an account, replacing any existing transactions for that account.
    /// </summary>
    /// <param name="account"></param>
    /// <param name="newTransactions"></param>
    private void SetTransactionsWithoutSplits(string account, IEnumerable<Transaction> newTransactions)
    {
        // Get transactions for other accounts and all splits.
        var otherTransactions = transactions.Where(t => t.Account != account || !string.IsNullOrEmpty(t.Split));

        transactions = Sort(otherTransactions.Concat(newTransactions));
    }    

    /// <summary>
    /// Try to sort a sequence of transactions that are consistent with the current balance.
    /// </summary>
    /// <param name="remaining"></param>
    /// <param name="currentBalance"></param>
    /// <param name="sequence"></param>
    /// <returns></returns>
    private static bool TrySortTransactions(List<Transaction> remaining, double currentBalance, List<Transaction> sequence)
    {
        if (remaining.Count == 0)
            return true;

        for (int i = 0; i < remaining.Count; i++)
        {
            var next = remaining[i];
            if (Math.Abs(next.PreviousBalance - currentBalance) >= 0.01)
                continue;

            remaining.RemoveAt(i);
            sequence.Add(next);
            if (TrySortTransactions(remaining, next.Balance, sequence))
                return true;

            sequence.RemoveAt(sequence.Count - 1);
            remaining.Insert(i, next);
        }

        return false;
    }

    /// <summary>
    /// Find the first transaction in a sequence of transactions. 
    /// The first transaction is the one that has no previous balance in the sequence.
    /// </summary>
    /// <param name="transactions"></param>
    public static Transaction FindFirstTransaction(IEnumerable<Transaction> transactions)
    {
        foreach (var transaction in transactions)
        {
            if (transactions.Except([transaction])
                            .FirstOrDefault(t => string.IsNullOrEmpty(t.Split) && t.Balance == transaction.PreviousBalance) == null)
                return transaction;
        }

        throw new Exception($"Couldn't find first transaction for {transactions.First().Date:yyyy-MM-dd}.");
    }

    /// <summary>
    /// Use ML to predict categories for bank transactions.
    /// </summary>
    /// <remarks>
    /// https://github.com/jernejk/MLSample.SimpleTransactionTagging
    /// </remarks>
    /// <param name="transactions">The collection of transaction instances.</param>
    public void PredictCategories()
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

    /// <summary>
    /// Get the list of categories from the prediction engine.
    /// </summary>
    /// <param name="predictionEngine"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Class used to hold the predicted category and score for a transaction.
    /// </summary>
    private class TransactionPrediction
    {
        [ColumnName("PredictedLabel")]
        public string? Category { get; set; }

        public float[]? Score { get; set; }
    }    
}
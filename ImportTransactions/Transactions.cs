using Microsoft.ML;
using Microsoft.ML.Data;
using MoreLinq;

namespace Finance;

/// <summary>
/// Methods that work on collections of transactions.
/// </summary>
public class Transactions
{
    /// <summary>
    /// Merge 2 collections of transactions
    /// </summary>
    /// <param name="first">The first collection.</param>
    /// <param name="second">The second collection.</param>
    /// <returns>A merged collection (items from second collected added to first collection)</returns>
    public static IEnumerable<Transaction> Merge(IEnumerable<Transaction> existingTransactions, IEnumerable<Transaction> newTransactions)
    {
        // Find the first matching transaction
        var firstMatchingTransaction = existingTransactions.FirstOrDefault(t => newTransactions.First().Equals(t)) ?? throw new Exception("Cannot find any overlap between the transactions being imported and the existing ones.");

        // Zip the existing and imported transactions.
        var zippedTransactions = existingTransactions.SkipWhile(t => t != firstMatchingTransaction)
                                                     .ZipLongest(newTransactions, (t1,t2) => (t1,t2));

        // Find the first transaction that differs.
        (Transaction? t1, Transaction? t2) = zippedTransactions.FirstOrDefault((t) => t.t1 == null ? true : !t.t1.Equals(t.t2));
        
        // Determine the existing transactions we want to keep.
        var existingTransactionsToKeep = existingTransactions.TakeWhile(t => t != t1).ToList();

        // Determine the existing transactions we want to remove.
        var existingTransactionsToRemove = existingTransactions.SkipWhile(t => t != t1).ToList();
        
        // Determine the new transactions we want to keep.
        var newTransactionsToKeep = newTransactions.SkipWhile(t => t != t2);

        // For the new transactions we want to keep, try and find a category from the existing transactions we want to remove.
        // i.e. assume the date has changed to a newer date.
        foreach (var newTransaction in newTransactionsToKeep)
        {
            var matchingTransaction = existingTransactionsToRemove.Find( (t) => t.Equals(newTransaction, useDate: false));
            if (matchingTransaction != null)
                newTransaction.Category = matchingTransaction.Category;
        }

        return existingTransactionsToKeep.Concat(newTransactionsToKeep);
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
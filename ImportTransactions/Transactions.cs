using Microsoft.ML;
using Microsoft.ML.Data;

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
    public static IEnumerable<Transaction> Merge(IEnumerable<Transaction> first, IEnumerable<Transaction> second)
    {
        if (second.Any())
        {
            if (first == null)
                return second;
            else
                return first.Union(second);
        }
        return first;
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
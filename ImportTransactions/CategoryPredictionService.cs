﻿using ImportTransactions;
using Microsoft.Extensions.ML;
using Microsoft.ML;
using Microsoft.ML.Data;
// https://github.com/jernejk/MLSample.SimpleTransactionTagging
namespace MLSample.TransactionTagging.Core
{
    public class CategoryPredictionService
    {
        private readonly MLContext _mlContext;
        private readonly PredictionEnginePool<Transaction, TransactionPrediction> _predictionEnginePool;

        private ITransformer _mlModel;
        private List<string> _categories;

        public static void Predict(IEnumerable<Transaction> transactions)
        {
            var transactionsWithCategories = transactions.Where(t => !string.IsNullOrEmpty(t.Category));

            var mlContext = new MLContext(0);
            var trainingService = new BankTransactionTrainingService(mlContext);
            var mlModel = trainingService.ManualTrain(transactionsWithCategories);

            var labelService = new CategoryPredictionService(mlContext);
            labelService.SetModel(mlModel);

            var categories = labelService.GetCategories();

            foreach (var transaction in transactions.Where(t => string.IsNullOrEmpty(t.Category)))
            {
                var prediction = labelService.Predict(transaction);

                var index = categories.IndexOf(prediction.Category);
                if (prediction.Score[index] > 0.5)
                {
                    transaction.Category = prediction.Category;
                    Console.WriteLine($"Ref: {transaction.Reference}. Predicted category: {prediction.Category}. Score: {prediction.Score[index]}");
                }
            }
        }


        public CategoryPredictionService(MLContext mlContext)
        {
            // Use this when trying to load models manually.
            _mlContext = mlContext;
        }

        public CategoryPredictionService(PredictionEnginePool<Transaction, TransactionPrediction> predictionEnginePool)
        {
            // Use this when using ML.NET in WebAPI, Azure Functions and other scalable applications.
            _predictionEnginePool = predictionEnginePool;
        }

        public void LoadModelFromFile(string modelPath)
        {
            // Load model from file.
            using (var stream = new FileStream(modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                SetModel(_mlContext.Model.Load(stream, out var _));
        }

        public void LoadModelFromStream(Stream modelStream)
        {
            // Load model from file.
            SetModel(_mlContext.Model.Load(modelStream, out var _));
        }

        public void SetModel(ITransformer mlModel)
        {
            _categories = null;
            _mlModel = mlModel;
        }

        public string PredictCategory(Transaction transaction)
        {
            var prediction = Predict(transaction);
            return prediction?.Category;
        }

        public TransactionPrediction Predict(Transaction transaction)
        {
            if (_predictionEnginePool != null)
            {
                // Used for scalable applications.
                return _predictionEnginePool.Predict(transaction);
            }

            // Used for console applications where multi-threading might not be a problem.
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<Transaction, TransactionPrediction>(_mlModel);
            return predictionEngine.Predict(transaction);
        }

        public List<string> GetCategories()
        {
            if (_categories != null)
            {
                return _categories;
            }

            // Based on https://github.com/dotnet/docs/issues/14265
            var schema = GetOutputSchema();
            var column = schema.GetColumnOrNull("Score");

            var slotNames = new VBuffer<ReadOnlyMemory<char>>();
            column.Value.GetSlotNames(ref slotNames);
            var names = new string[slotNames.Length];

            _categories = slotNames
                .DenseValues()
                .Select(x => x.ToString())
                .ToList();

            return _categories;
        }

        public DataViewSchema GetOutputSchema()
        {
            PredictionEngine<Transaction, TransactionPrediction> predEngine = _predictionEnginePool != null
                ? _predictionEnginePool.GetPredictionEngine()
                : _mlContext.Model.CreatePredictionEngine<Transaction, TransactionPrediction>(_mlModel);

            return predEngine.OutputSchema;
        }

        public static Dictionary<string, float> GetScoresWithLabelsSorted(DataViewSchema schema, string name, float[] scores)
        {
            // Based on https://github.com/dotnet/docs/issues/14265
            Dictionary<string, float> result = new Dictionary<string, float>();

            var column = schema.GetColumnOrNull(name);

            var slotNames = new VBuffer<ReadOnlyMemory<char>>();
            column.Value.GetSlotNames(ref slotNames);
            var names = new string[slotNames.Length];
            var num = 0;
            foreach (var denseValue in slotNames.DenseValues())
            {
                result.Add(denseValue.ToString(), scores[num++]);
            }

            return result.OrderByDescending(c => c.Value).ToDictionary(i => i.Key, i => i.Value);
        }
    }
}
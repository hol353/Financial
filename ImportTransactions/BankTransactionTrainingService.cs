using Microsoft.ML;
using Microsoft.ML.AutoML;

namespace Finance
{
    /// <summary>
    /// A training service for an based bank transaction category prediction.
    /// </summary>
    public class BankTransactionTrainingService
    {
        private readonly MLContext _mlContext;
        private IDataView? _trainingDataView;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mlContext">The ML context.</param>
        public BankTransactionTrainingService(MLContext mlContext)
        {
            _mlContext = mlContext;
        }

        /// <summary>
        /// Manually train the ML engine.
        /// </summary>
        /// <param name="trainingData">The training data to use.</param>
        /// <returns>A transformer that can be used to do the prediction.</returns>
        public ITransformer ManualTrain(IEnumerable<Transaction> trainingData)
        {
            // Configure ML pipeline
            var pipeline = LoadDataProcessPipeline(_mlContext);
            var trainingPipeline = GetTrainingPipeline(_mlContext, pipeline);
            _trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Generate training model.
            return trainingPipeline.Fit(_trainingDataView);
        }

        /// <summary>
        /// An auto-trainer method.
        /// </summary>
        /// <param name="trainingData">The training data to use.</param>
        /// <param name="maxTimeInSec">Maximum amount of time (sec) for the training.</param>
        /// <returns></returns>
        public ITransformer AutoTrain(IEnumerable<Transaction> trainingData, uint maxTimeInSec)
        {
            _trainingDataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            var experimentSettings = new MulticlassExperimentSettings();
            experimentSettings.MaxExperimentTimeInSeconds = maxTimeInSec;
            experimentSettings.OptimizingMetric = MulticlassClassificationMetric.MacroAccuracy;

            var experiment = _mlContext.Auto().CreateMulticlassClassificationExperiment(experimentSettings);
            var columnInfo = new ColumnInformation
            {
                LabelColumnName = nameof(Transaction.Category)
            };
            columnInfo.TextColumnNames.Add(nameof(Transaction.Reference));

            var result = experiment.Execute(_trainingDataView, columnInfo);
            return result.BestRun.Model;
        }

        private IEstimator<ITransformer> LoadDataProcessPipeline(MLContext mlContext)
        {
            // Configure data pipeline based on the features in TransactionData.
            // Description and TransactionType are the inputs and Category is the expected result.
            var dataProcessPipeline = mlContext
                .Transforms.Conversion.MapValueToKey(inputColumnName: nameof(Transaction.Category), outputColumnName: "Label")
                .Append(mlContext.Transforms.Text.FeaturizeText(inputColumnName: nameof(Transaction.Reference), outputColumnName: "Features"))
                .AppendCacheCheckpoint(mlContext);

            return dataProcessPipeline;
        }

        private IEstimator<ITransformer> GetTrainingPipeline(MLContext mlContext, IEstimator<ITransformer> pipeline)
        {
            // Use the multi-class SDCA algorithm to predict the label using features.
            // For StochasticDualCoordinateAscent the KeyToValue needs to be PredictedLabel.
            return pipeline
                .Append(GetScadaTrainer(mlContext))
                .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));
        }

        private Microsoft.ML.Trainers.SdcaMaximumEntropyMulticlassTrainer GetScadaTrainer(MLContext mlContext)
        {
            return mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features");
        }
    }
}
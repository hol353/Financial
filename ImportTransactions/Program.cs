using System.Reflection;
using ImportTransactions;
using MLSample.TransactionTagging.Core;

/// Main entry point
try
{
    string? directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Invalid assembly location");
    string fileName = Path.Combine(directoryName, "Cash book.xlsx");

    var transactions = Excel.Read<Transaction>(fileName, "Transactions");
    var transactionsToImport = BankTransactionFile.Read(args);
    foreach (var transaction in transactionsToImport)
        transaction.Category = null;
    var mergedTransactions = Transactions.Merge(transactions, transactionsToImport);
    CategoryPredictionService.Predict(mergedTransactions);
    Excel.Write(fileName, "Transactions", mergedTransactions);
}
catch (Exception err)
{
    Console.WriteLine(err.ToString());
}

Console.Write("Press any key... ");
Console.ReadKey();
using System.Reflection;
using ImportTransactions;

/// Main entry point
try
{
    string? directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? throw new Exception("Invalid assembly location");
    string fileName = Path.Combine(directoryName, "Cash book.xlsx");

    var transactions = Excel.Read<Transaction>(fileName, "Transactions");
    var transactionsToImport = BankTransactionFile.Read(args);

    // temporary write to csv 
    
    Csv.Write(Path.Combine(directoryName, "TransactionsToImport.csv"), transactionsToImport);

    var mergedTransactions = Transactions.Merge(transactions, transactionsToImport);
    Excel.Write(fileName, "Transactions", mergedTransactions);
}
catch (Exception err)
{
    Console.WriteLine(err.ToString());
}

Console.Write("Press any key... ");
Console.ReadKey();
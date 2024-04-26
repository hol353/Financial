using System.Linq.Expressions;
using System.Reflection;
using System.Transactions;
using ImportTransactions;


try
{
    string? directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    if (directoryName == null)
    throw new Exception("Invalid assembly location");

    string fileName = Path.Combine(directoryName, "Cash book.xlsx");
    if (!File.Exists(fileName))
        throw new Exception($"Cannot find file {fileName}");
    Transactions transactions = new(fileName);

    foreach (var arg in args)
    {
        Console.WriteLine($"Importing {arg}...");
        transactions.Import(arg);
    }

    Console.WriteLine($"Saving {fileName}");
    transactions.Write();
}
catch (Exception err)
{
    Console.WriteLine(err.ToString());
}

Console.Write("Press any key... ");
Console.ReadKey();
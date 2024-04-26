namespace ImportTransactions;

public class Transactions
{
    private string filePath;

    public Transactions(string fileName)
    {
        filePath = fileName;
        if (File.Exists(fileName))
        {
            if (Path.GetExtension(fileName) == ".xlsx")
                AllTransactions = Excel.Read<Transaction>(fileName, sheetName: "Transactions");
            else
                AllTransactions = Csv.Read<Transaction>(fileName);
        }
    }

    public IEnumerable<Transaction>? AllTransactions { get; set; }


    public void Import(string fileName)
    {
        // Read bank file.
        var bankTransactions = BankTransactionFile.Read(fileName);

        if (bankTransactions != null)
        {
            // Merge dataTable with our Transactions DataTable
            if (AllTransactions == null)
                AllTransactions = bankTransactions;
            else
            {
                var dupComparer = new InlineComparer<Transaction>((t1, t2) => t1.Account == t2.Account && t1.Date == t2.Date && t1.Amount == t2.Amount && CompareReference(t1, t2), 
                                                                         i => i.Account.GetHashCode() + i.Date.GetHashCode() + i.Amount.GetHashCode() );

                AllTransactions = AllTransactions.Union(bankTransactions, dupComparer);
            }
        }
    }

    public class InlineComparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> getEquals;
            private readonly Func<T, int> getHashCode;

            public InlineComparer(Func<T, T, bool> equals, Func<T, int> hashCode)
            {
                getEquals = equals;
                getHashCode = hashCode;
            }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool Equals(T x, T y)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        {
                return getEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return getHashCode(obj);
            }
        }    

    private bool CompareReference(Transaction t1, Transaction t2)
    {
        string[] t1Tokens = t1.Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string[] t2Tokens = t2.Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        double percent = t1Tokens.Count(t => t2Tokens.Contains(t)) * 1.0 / t1Tokens.Length * 100;
        return percent > 50;
    }

    // private IEnumerable<Transaction> FilterRowsToKeep(IEnumerable<Transaction> bankTransactions)
    // {
    //     if (transactions == null)
    //         return bankTransactions;
            
    //     string account = bankTransactions.First().Account;

    //     // Find the last stored transaction for the matching account
    //     Transaction? lastTransaction = transactions.Last(r => r.Account == account);

    //     // Look for the matching row in the bank transactions.
    //     Transaction? lastBankTransaction = bankTransactions.First(r => r.Date == lastTransaction.Date &&
    //                                                                    r.Amount == lastTransaction.Amount &&
    //                                                                    r.Reference == lastTransaction.Reference);
    //     if (lastBankTransaction == null)
    //         throw new Exception("Cannot find matching row in bank file");

    //     return bankTransactions.AsEnumerable().Skip(bankTransactions.Rows.IndexOf(row)+1);
    // }

    public void Write()
    {
        if (AllTransactions != null)
        {
            if (Path.GetExtension(filePath) == ".xlsx")
                Excel.Write(filePath, "Transactions", AllTransactions);
            else
                Csv.Write(filePath, AllTransactions);            
            
        }
    }
}

namespace Finance;

public sealed class TransactionEqualityComparer : IEqualityComparer<Transaction>
{
    public static TransactionEqualityComparer Instance { get; } = new();

    public bool Equals(Transaction? x, Transaction? y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        bool equal = x.Account == y.Account
            && x.Date == y.Date
            && x.Amount.Equals(y.Amount)
            && x.Split == y.Split
            && x.Reference == y.Reference
            && x.Balance.Equals(y.Balance)
            && x.Category == y.Category
            && x.Details == y.Details
            && x.InvoiceReceipt == y.InvoiceReceipt;
        return equal;
    }

    public int GetHashCode(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var hash = new HashCode();
        hash.Add(transaction.Account);
        hash.Add(transaction.Date);
        hash.Add(transaction.Amount);
        hash.Add(transaction.Split);
        hash.Add(transaction.Reference);
        hash.Add(transaction.Balance);
        hash.Add(transaction.Category);
        hash.Add(transaction.Details);
        hash.Add(transaction.InvoiceReceipt);
        return hash.ToHashCode();
    }
}

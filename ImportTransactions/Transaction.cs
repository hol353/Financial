namespace ImportTransactions;

/// <summary>
/// Encapsulates a single bank transaction, with equality functionality.
/// </summary>
public class Transaction : IEquatable<Transaction>
{
    public string Account { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public double Amount { get; set; }

    public string Reference { get; set; } = string.Empty;

    public double Balance { get; set; }

    /// <summary>
    /// Tests equality between this transaction and another.
    /// </summary>
    /// <param name="other">The other transaction.</param>
    /// <returns>True if equal</returns>
    public bool Equals(Transaction? other)
    {
        if (other == null)
            throw new Exception("other is null");
        if (Account == other.Account && Date == other.Date && Amount == other.Amount)
        {
            string[] tokens = Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] otherTokens = other.Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double percent = tokens.Count(t => otherTokens.Contains(t)) * 1.0 / tokens.Length * 100;
            return percent > 50;
        }
        return false;
    }

    /// <summary>
    /// Tests equality between this transaction and another.
    /// </summary>
    /// <param name="other">The other transaction.</param>
    /// <returns>True if equal</returns>
    public override bool Equals(object? obj) => Equals(obj as Transaction);

    /// <summary>
    /// Calculates a hash code for this transaction.
    /// </summary>
    /// <param name="other">The other transaction.</param>
    /// <returns>True if equal</returns>    
    public override int GetHashCode() => (Account, Date, Amount).GetHashCode();
}
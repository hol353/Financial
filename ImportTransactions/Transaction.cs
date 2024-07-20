namespace Finance;

/// <summary>
/// Encapsulates a single bank transaction, with equality functionality.
/// </summary>
public class Transaction : IEquatable<Transaction>
{
    /// <summary>The account number.</summary>
    public string Account { get; set; } = string.Empty;

    /// <summary>The date of the transaction.</summary>
    public DateTime Date { get; set; }

    /// <summary>The amount of the transaction.</summary>
    public double Amount { get; set; }

    /// <summary>The reference (description) of the transaction.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>The balance of the account.</summary>
    public double Balance { get; set; }

    /// <summary>The category of the transaction.</summary>
    public string Category { get; set; } = string.Empty;    

    /// <summary>
    /// Tests equality between this transaction and another.
    /// </summary>
    /// <param name="other">The other transaction.</param>
    /// <param name="useDate">Use date to do match?</param>
    /// <returns>True if equal</returns>
    public bool Equals(Transaction? other)
    {
        return Equals(other, useDate: true);
    }

    /// <summary>
    /// Tests equality between this transaction and another.
    /// </summary>
    /// <param name="other">The other transaction.</param>
    /// <param name="useDate">Use date to do match?</param>
    /// <returns>True if equal</returns>
    public bool Equals(Transaction? other, bool useDate)
    {
        if (other == null)
            throw new Exception("other is null");
        if (Account == other.Account && Amount == other.Amount && 
            (!useDate || Date == other.Date))
        {
            string[] tokens = Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] otherTokens = other.Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double percent = tokens.Count(t => otherTokens.Contains(t)) * 1.0 / tokens.Length * 100;
            return percent >= 50;
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
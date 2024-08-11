namespace Finance;

/// <summary>
/// Encapsulates a single bank transaction, with equality functionality.
/// </summary>
public class Transaction //: IEquatable<Transaction>
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

    /// <summary>The details of the transaction.</summary>
    public string Details { get; set; } = string.Empty;  

    /// <summary>The details of the transaction.</summary>
    public string InvoiceReceipt { get; set; } = string.Empty;      

    /// <summary>
    /// Tests equality between this transaction and another.
    /// </summary>
    /// <param name="other">The other transaction.</param>
    /// <param name="exactDate">Dates must match exactly?</param>
    /// <returns>True if equal</returns>
    public bool CloseMatch(Transaction? other)
    {
        if (other == null)
            throw new Exception("other is null");
        if (Account == other.Account && Amount == other.Amount && 
            (other.Date - Date).Days < 10)
        {
            string[] tokens = Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string[] otherTokens = other.Reference.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            double percent = tokens.Count(t => otherTokens.Contains(t)) * 1.0 / tokens.Length * 100;
            return percent >= 50;
        }
        return false;
    }
 }
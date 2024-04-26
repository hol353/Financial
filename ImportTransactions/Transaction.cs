
namespace ImportTransactions;

public class Transaction
{
    public string Account { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public double Amount { get; set; }

    public string Reference { get; set; } = string.Empty;

    public double Balance { get; set; }

}
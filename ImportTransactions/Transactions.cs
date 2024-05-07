using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.TorchSharp;
using Microsoft.ML.TorchSharp.NasBert;

namespace ImportTransactions;

/// <summary>
/// Methods that work on collections of transactions.
/// </summary>
public class Transactions
{
    /// <summary>
    /// Merge 2 collections of transactions
    /// </summary>
    /// <param name="first">The first collection.</param>
    /// <param name="second">The second collection.</param>
    /// <returns>A merged collection (items from second collected added to first collection)</returns>
    public static IEnumerable<Transaction> Merge(IEnumerable<Transaction> first, IEnumerable<Transaction> second)
    {
        if (second.Any())
        {
            if (first == null)
                return second;
            else
                return first.Union(second);
        }
        return first;
    }
}
using Finance;

namespace Tests;

[TestClass]
public class TestTransactions
{

    /// <summary>
    /// Test the simple case where transactions are merged when they are identical.
    /// </summary>
    [TestMethod]
    public void TestMergeIntoExistingTransactions()
    {
        List<Transaction> existingTransactions = 
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 500, Account = "xxxx", Reference = "1", Balance = 5001.30 },
                new() { Date = new DateTime(2024, 3, 25), Amount = 260, Account = "xxxx", Reference = "2", Balance = 6031.30 },
                new() { Date = new DateTime(2024, 3, 29), Amount = -1980, Account = "xxxx", Reference = "3", Balance = 4051.30 },
                new() { Date = new DateTime(2024, 4, 2), Amount = 10000, Account = "xxxx", Reference = "4", Balance = 14051.30 }                
            ];
            
        List<Transaction> newTransactions = 
            [
                new() { Date = new DateTime(2024, 3, 29), Amount = -1980, Account = "xxxx", Reference = "3", Balance = 4051.30 },
                new() { Date = new DateTime(2024, 4, 02), Amount = 10000, Account = "xxxx", Reference = "4", Balance = 14051.30 },
                new() { Date = new DateTime(2024, 4, 05), Amount = 512, Account = "xxxx", Reference = "5", Balance = 6666.40 },
                new() { Date = new DateTime(2024, 4, 08), Amount = 234, Account = "xxxx", Reference = "6", Balance = 35001.30 }                
            ];

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();
        if (mergedTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual("xxxx", mergedTransactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), mergedTransactions[0].Date);
        Assert.AreEqual(500.0, mergedTransactions[0].Amount);
        Assert.AreEqual("1", mergedTransactions[0].Reference);
        Assert.AreEqual(5001.3, mergedTransactions[0].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,25), mergedTransactions[1].Date);
        Assert.AreEqual(260.0, mergedTransactions[1].Amount);
        Assert.AreEqual("2", mergedTransactions[1].Reference);
        Assert.AreEqual(6031.3, mergedTransactions[1].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[2].Account);
        Assert.AreEqual(new DateTime(2024,3,29), mergedTransactions[2].Date);
        Assert.AreEqual(-1980.0, mergedTransactions[2].Amount);
        Assert.AreEqual("3", mergedTransactions[2].Reference);
        Assert.AreEqual(4051.3, mergedTransactions[2].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[3].Account);
        Assert.AreEqual(new DateTime(2024,4,2), mergedTransactions[3].Date);
        Assert.AreEqual(10000.0, mergedTransactions[3].Amount);
        Assert.AreEqual("4", mergedTransactions[3].Reference);
        Assert.AreEqual(14051.3, mergedTransactions[3].Balance);


        Assert.AreEqual("xxxx", mergedTransactions[4].Account);
        Assert.AreEqual(new DateTime(2024,4,5), mergedTransactions[4].Date);
        Assert.AreEqual(512.0, mergedTransactions[4].Amount);
        Assert.AreEqual("5", mergedTransactions[4].Reference);
        Assert.AreEqual(6666.4, mergedTransactions[4].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[5].Account);
        Assert.AreEqual(new DateTime(2024,4,8), mergedTransactions[5].Date);
        Assert.AreEqual(234.0, mergedTransactions[5].Amount);
        Assert.AreEqual("6", mergedTransactions[5].Reference);
        Assert.AreEqual(35001.3, mergedTransactions[5].Balance);
    }

    /// <summary>
    /// Test the case where transactions should be considered the same but the references are a bit different.
    /// Some banks modify the reference field some days after the original transaction.
    /// </summary>
    [TestMethod]
    public void TestImportWhereTransactionsAreSameButDifferentReferences()
    {
        List<Transaction> existingTransactions = 
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 500, Account = "xxxx", Reference = "yyyy4 asdf", Balance = 5001.30 },
                new() { Date = new DateTime(2024, 3, 21), Amount = -1812, Account = "xxxx", Reference = "BPAY BA67347594575 TRC RATES", Balance = 2733.05 }         
            ];

        List<Transaction> newTransactions = 
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 500, Account = "xxxx", Reference = "yyyy4 asdf Extra info", Balance = 5001.30 },
                new() { Date = new DateTime(2024, 3, 21), Amount = -1812, Account = "xxxx", Reference = "INTERNET BPAY TRC RATES 5789540", Balance = 2733.05 }         
            ];

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();
        if (mergedTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual(2, mergedTransactions.Count());
        Assert.AreEqual("xxxx", mergedTransactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), mergedTransactions[0].Date);
        Assert.AreEqual(500.0, mergedTransactions[0].Amount);
        Assert.AreEqual("yyyy4 asdf", mergedTransactions[0].Reference); // transaction not changed via import.
        Assert.AreEqual(5001.3, mergedTransactions[0].Balance);

        Assert.AreEqual(2, mergedTransactions.Count());
        Assert.AreEqual("xxxx", mergedTransactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,21), mergedTransactions[1].Date);
        Assert.AreEqual(-1812.0, mergedTransactions[1].Amount);
        Assert.AreEqual("BPAY BA67347594575 TRC RATES", mergedTransactions[1].Reference); // transaction not changed via import.
        Assert.AreEqual(2733.05, mergedTransactions[1].Balance);
    }    

    /// <summary>
    /// Some banks change the date of their transactions between one import and the next.
    /// </summary>
    [TestMethod]
    public void TestDetectChangedBankTransactions()
    {
        List<Transaction> existingTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 123, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 456, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 789, Account = "S1", Reference = "c", Category = "c3" }            
        ];
        
        // New transactions don't have a category and the second existing transaction (2/6/2024) has a new date (4/6/2024)
        List<Transaction> newTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 123, Account = "S1", Reference = "a", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 3), Amount = 789, Account = "S1", Reference = "c", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 4), Amount = 456, Account = "S1", Reference = "b", Category = string.Empty }  
        ];

        // TODO: Ensure the above transaction lists have a different number of transactions. 

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();
        if (mergedTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual(3, mergedTransactions.Count());
        Assert.AreEqual(new DateTime(2024, 6, 1), mergedTransactions[0].Date);
        Assert.AreEqual(123.0, mergedTransactions[0].Amount);
        Assert.AreEqual("S1", mergedTransactions[0].Account);
        Assert.AreEqual("a", mergedTransactions[0].Reference); 
        Assert.AreEqual("c1", mergedTransactions[0].Category);

        Assert.AreEqual(new DateTime(2024, 6, 3), mergedTransactions[1].Date);
        Assert.AreEqual(789.0, mergedTransactions[1].Amount);
        Assert.AreEqual("S1", mergedTransactions[1].Account);
        Assert.AreEqual("c", mergedTransactions[1].Reference); 
        Assert.AreEqual("c3", mergedTransactions[1].Category);

        Assert.AreEqual(new DateTime(2024, 6, 4), mergedTransactions[2].Date);
        Assert.AreEqual(456.0, mergedTransactions[2].Amount);
        Assert.AreEqual("S1", mergedTransactions[2].Account);
        Assert.AreEqual("b", mergedTransactions[2].Reference); 
        Assert.AreEqual("c2", mergedTransactions[2].Category);        
    }        
}
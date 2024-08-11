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
                new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "1", Balance = 100 },
                new() { Date = new DateTime(2024, 3, 25), Amount = 20, Account = "xxxx", Reference = "2", Balance = 120 },
                new() { Date = new DateTime(2024, 3, 29), Amount = 30, Account = "xxxx", Reference = "3", Balance = 150 },
                new() { Date = new DateTime(2024, 4, 2), Amount = 40, Account = "xxxx", Reference = "4", Balance = 190 }                
            ];
            
        List<Transaction> newTransactions = 
            [
                new() { Date = new DateTime(2024, 3, 29), Amount = 30, Account = "xxxx", Reference = "3", Balance = 150 },
                new() { Date = new DateTime(2024, 4, 02), Amount = 40, Account = "xxxx", Reference = "4", Balance = 190 },
                new() { Date = new DateTime(2024, 4, 05), Amount = 50, Account = "xxxx", Reference = "5", Balance = 240 },
                new() { Date = new DateTime(2024, 4, 08), Amount = 60, Account = "xxxx", Reference = "6", Balance = 300 }                
            ];

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();
        if (mergedTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual("xxxx", mergedTransactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), mergedTransactions[0].Date);
        Assert.AreEqual(10, mergedTransactions[0].Amount);
        Assert.AreEqual("1", mergedTransactions[0].Reference);
        Assert.AreEqual(100, mergedTransactions[0].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,25), mergedTransactions[1].Date);
        Assert.AreEqual(20.0, mergedTransactions[1].Amount);
        Assert.AreEqual("2", mergedTransactions[1].Reference);
        Assert.AreEqual(120, mergedTransactions[1].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[2].Account);
        Assert.AreEqual(new DateTime(2024,3,29), mergedTransactions[2].Date);
        Assert.AreEqual(30, mergedTransactions[2].Amount);
        Assert.AreEqual("3", mergedTransactions[2].Reference);
        Assert.AreEqual(150, mergedTransactions[2].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[3].Account);
        Assert.AreEqual(new DateTime(2024,4,2), mergedTransactions[3].Date);
        Assert.AreEqual(40, mergedTransactions[3].Amount);
        Assert.AreEqual("4", mergedTransactions[3].Reference);
        Assert.AreEqual(190, mergedTransactions[3].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[4].Account);
        Assert.AreEqual(new DateTime(2024,4,5), mergedTransactions[4].Date);
        Assert.AreEqual(50, mergedTransactions[4].Amount);
        Assert.AreEqual("5", mergedTransactions[4].Reference);
        Assert.AreEqual(240, mergedTransactions[4].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[5].Account);
        Assert.AreEqual(new DateTime(2024,4,8), mergedTransactions[5].Date);
        Assert.AreEqual(60, mergedTransactions[5].Amount);
        Assert.AreEqual("6", mergedTransactions[5].Reference);
        Assert.AreEqual(300, mergedTransactions[5].Balance);
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
                new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "yyyy4 asdf", Balance = 100 },
                new() { Date = new DateTime(2024, 3, 21), Amount = 20, Account = "xxxx", Reference = "BPAY BA67347594575 TRC RATES", Balance = 120}         
            ];

        List<Transaction> newTransactions = 
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "yyyy4 asdf", Balance = 100 },
                new() { Date = new DateTime(2024, 3, 21), Amount = 20, Account = "xxxx", Reference = "INTERNET BPAY TRC RATES 5789540", Balance = 120 }         
            ];

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();
        if (mergedTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual(2, mergedTransactions.Count());
        Assert.AreEqual("xxxx", mergedTransactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), mergedTransactions[0].Date);
        Assert.AreEqual(10, mergedTransactions[0].Amount);
        Assert.AreEqual("yyyy4 asdf", mergedTransactions[0].Reference); // reference changed via import.
        Assert.AreEqual(100, mergedTransactions[0].Balance);

        Assert.AreEqual(2, mergedTransactions.Count());
        Assert.AreEqual("xxxx", mergedTransactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,21), mergedTransactions[1].Date);
        Assert.AreEqual(20, mergedTransactions[1].Amount);
        Assert.AreEqual("INTERNET BPAY TRC RATES 5789540", mergedTransactions[1].Reference); // reference changed via import.
        Assert.AreEqual(120, mergedTransactions[1].Balance);
    }    

    /// <summary>
    /// Some banks change the date of their transactions between one import and the next.
    /// </summary>
    [TestMethod]
    public void TestDetectRedatedBankTransactions()
    {
        List<Transaction> existingTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 20, Balance = 120, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 150, Account = "S1", Reference = "c", Category = "c3" }            
        ];
        
        // New transactions don't have a category and the second and 3rd transactions have new dates.
        List<Transaction> newTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 4), Amount = 20, Balance = 120, Account = "S1", Reference = "b", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 5), Amount = 30, Balance = 150, Account = "S1", Reference = "c", Category = string.Empty },
        ];

        // TODO: Ensure the above transaction lists have a different number of transactions. 

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();

        Assert.AreEqual(3, mergedTransactions.Count());
        Assert.AreEqual(new DateTime(2024, 6, 1), mergedTransactions[0].Date);
        Assert.AreEqual(10, mergedTransactions[0].Amount);
        Assert.AreEqual(100, mergedTransactions[0].Balance);
        Assert.AreEqual("S1", mergedTransactions[0].Account);
        Assert.AreEqual("a", mergedTransactions[0].Reference); 
        Assert.AreEqual("c1", mergedTransactions[0].Category);

        Assert.AreEqual(new DateTime(2024, 6, 4), mergedTransactions[1].Date);
        Assert.AreEqual(20, mergedTransactions[1].Amount);
        Assert.AreEqual(120, mergedTransactions[1].Balance);
        Assert.AreEqual("S1", mergedTransactions[1].Account);
        Assert.AreEqual("b", mergedTransactions[1].Reference); 
        Assert.AreEqual("c2", mergedTransactions[1].Category);        

        Assert.AreEqual(new DateTime(2024, 6, 5), mergedTransactions[2].Date);
        Assert.AreEqual(30, mergedTransactions[2].Amount);
        Assert.AreEqual(150, mergedTransactions[2].Balance);
        Assert.AreEqual("S1", mergedTransactions[2].Account);
        Assert.AreEqual("c", mergedTransactions[2].Reference); 
        Assert.AreEqual("c3", mergedTransactions[2].Category);
    }        


    /// <summary>
    /// It is possible a re-dated transaction is beyond the date range of existing transactions.
    /// </summary>
    [TestMethod]
    public void TestDetectRedatedBankPastEndExisting()
    {
        List<Transaction> existingTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 990, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 20, Balance = 1010, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 1040, Account = "S1", Reference = "c", Category = "c3" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 40, Balance = 1080, Account = "S1", Reference = "d", Category = "c4" },
            new() { Date = new DateTime(2024, 6, 13), Amount = 50, Balance = 1130, Account = "S1", Reference = "e", Category = "c5" }            
        ];
        
        // New transactions don't have a category and the second existing transaction (2/6/2024) has a new date (9/6/2024)
        // Also note the new transactions have adjusted bank balances to match the new order. This is what the bank does.
        List<Transaction> newTransactions = 
        [
            new() { Date = new DateTime(2024, 5, 30), Amount = 5,  Balance = 980, Account = "S1", Reference = "?", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 990, Account = "S1", Reference = "a", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 1020, Account = "S1", Reference = "c", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 3), Amount = 40, Balance = 1060, Account = "S1", Reference = "d", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 9), Amount = 20, Balance = 1080, Account = "S1", Reference = "b", Category = string.Empty }, // redated transaction.
            new() { Date = new DateTime(2024, 6, 13), Amount = 50, Balance = 1130, Account = "S1", Reference = "e", Category = string.Empty },
        ];

        // TODO: Ensure the above transaction lists have a different number of transactions. 

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();

        Assert.AreEqual(5, mergedTransactions.Count());
        Assert.AreEqual(new DateTime(2024, 6, 1), mergedTransactions[0].Date);
        Assert.AreEqual(10.0, mergedTransactions[0].Amount);
        Assert.AreEqual(990.0, mergedTransactions[0].Balance);
        Assert.AreEqual("S1", mergedTransactions[0].Account);
        Assert.AreEqual("a", mergedTransactions[0].Reference); 
        Assert.AreEqual("c1", mergedTransactions[0].Category);

        Assert.AreEqual(new DateTime(2024, 6, 3), mergedTransactions[1].Date);
        Assert.AreEqual(30, mergedTransactions[1].Amount);
        Assert.AreEqual(1020.0, mergedTransactions[1].Balance);
        Assert.AreEqual("S1", mergedTransactions[1].Account);
        Assert.AreEqual("c", mergedTransactions[1].Reference); 
        Assert.AreEqual("c3", mergedTransactions[1].Category);

        Assert.AreEqual(new DateTime(2024, 6, 3), mergedTransactions[2].Date);
        Assert.AreEqual(40, mergedTransactions[2].Amount);
        Assert.AreEqual(1060, mergedTransactions[2].Balance);
        Assert.AreEqual("S1", mergedTransactions[2].Account);
        Assert.AreEqual("d", mergedTransactions[2].Reference); 
        Assert.AreEqual("c4", mergedTransactions[2].Category);
                
        Assert.AreEqual(new DateTime(2024, 6, 9), mergedTransactions[3].Date);
        Assert.AreEqual(20, mergedTransactions[3].Amount);
        Assert.AreEqual(1080, mergedTransactions[3].Balance);
        Assert.AreEqual("S1", mergedTransactions[3].Account);
        Assert.AreEqual("b", mergedTransactions[3].Reference); 
        Assert.AreEqual("c2", mergedTransactions[3].Category);

        Assert.AreEqual(new DateTime(2024, 6, 13), mergedTransactions[4].Date);
        Assert.AreEqual(50, mergedTransactions[4].Amount);
        Assert.AreEqual(1130, mergedTransactions[4].Balance);
        Assert.AreEqual("S1", mergedTransactions[4].Account);
        Assert.AreEqual("e", mergedTransactions[4].Reference); 
        Assert.AreEqual("c5", mergedTransactions[4].Category);        
    }      

    /// <summary>
    /// A merge can fail if there is insufficient date overlap between imported and existing transactions.
    /// </summary>
    [TestMethod]
    public void TestInsufficientImportedTransactionOverlap()
    {
        List<Transaction> existingTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 1000, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 20, Balance = 1010, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 1030, Account = "S1", Reference = "c", Category = "c3" }            
        ];
        
        // No overlap between above existing transactions and below new transactions. Should throw.
        List<Transaction> newTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 1010, Account = "S1", Reference = "c", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 4), Amount = 40, Balance = 1050, Account = "S1", Reference = "d", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 5), Amount = 50, Balance = 1100, Account = "S1", Reference = "e", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 9), Amount = 20, Balance = 1120, Account = "S1", Reference = "b", Category = string.Empty }, // redated transaction.
        ];

        // TODO: Ensure the above transaction lists have a different number of transactions. 

        var mergedTransactions = Assert.ThrowsException<Exception>(() => Transactions.Merge(existingTransactions, newTransactions), 
                                                                   "Cannot import new transactions. There needs to be more overlap of existing and new transactions");
    }    

    /// <summary>
    /// If the order of the accounts change (only happens if I decide I want a different order)
    /// then should still work.
    /// </summary>
    [TestMethod]
    public void TestAccountOrderChanged()
    {
        List<Transaction> existingTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 111, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 222, Account = "S2", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 333, Account = "S3", Reference = "c", Category = "c3" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 444, Account = "S5", Reference = "d", Category = "c4" }
        ];
        
        // New transactions don't have a category and the second and 3rd transactions have new dates.
        List<Transaction> newTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 111, Account = "S1", Reference = "a", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 1), Amount = 444, Account = "S5", Reference = "d", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 1), Amount = 333, Account = "S3", Reference = "c", Category = string.Empty },
            new() { Date = new DateTime(2024, 6, 1), Amount = 222, Account = "S2", Reference = "b", Category = string.Empty },
        ];

        var mergedTransactions = Transactions.Merge(existingTransactions, newTransactions).ToArray();
        if (mergedTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual(4, mergedTransactions.Count());
        Assert.AreEqual(new DateTime(2024, 6, 1), mergedTransactions[0].Date);
        Assert.AreEqual(111.0, mergedTransactions[0].Amount);
        Assert.AreEqual("S1", mergedTransactions[0].Account);
        Assert.AreEqual("a", mergedTransactions[0].Reference); 
        Assert.AreEqual("c1", mergedTransactions[0].Category);

        Assert.AreEqual(new DateTime(2024, 6, 1), mergedTransactions[1].Date);
        Assert.AreEqual(222, mergedTransactions[1].Amount);
        Assert.AreEqual("S2", mergedTransactions[1].Account);
        Assert.AreEqual("b", mergedTransactions[1].Reference); 
        Assert.AreEqual("c2", mergedTransactions[1].Category);  

        Assert.AreEqual(new DateTime(2024, 6, 1), mergedTransactions[2].Date);
        Assert.AreEqual(333, mergedTransactions[2].Amount);
        Assert.AreEqual("S3", mergedTransactions[2].Account);
        Assert.AreEqual("c", mergedTransactions[2].Reference); 
        Assert.AreEqual("c3", mergedTransactions[2].Category);

        Assert.AreEqual(new DateTime(2024, 6, 1), mergedTransactions[3].Date);
        Assert.AreEqual(444, mergedTransactions[3].Amount);
        Assert.AreEqual("S5", mergedTransactions[3].Account);
        Assert.AreEqual("d", mergedTransactions[3].Reference); 
        Assert.AreEqual("c4", mergedTransactions[3].Category);        
    }     

    /// <summary>
    /// Test sorting - some banks don't have their transactions in order such that the balances make sense.
    /// </summary>
    [TestMethod]
    public void TestSort()
    {
        List<Transaction> transactions = 
        [
            new() { Date = new DateTime(2024, 6, 2), Amount = 40, Balance = 190, Account = "S1", Reference = "d" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 20, Balance = 120, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 60, Balance = 300, Account = "S1", Reference = "f" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 30, Balance = 150, Account = "S1", Reference = "c" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 50, Balance = 240, Account = "S1", Reference = "e" },
        ];

        var sortedTransactions = Transactions.Sort(transactions).ToArray();

        Assert.AreEqual(new DateTime(2024, 6, 1), sortedTransactions[0].Date);
        Assert.AreEqual(10, sortedTransactions[0].Amount);
        Assert.AreEqual(100, sortedTransactions[0].Balance);

        Assert.AreEqual(new DateTime(2024, 6, 1), sortedTransactions[1].Date);
        Assert.AreEqual(20, sortedTransactions[1].Amount);
        Assert.AreEqual(120, sortedTransactions[1].Balance);

        Assert.AreEqual(new DateTime(2024, 6, 2), sortedTransactions[2].Date);
        Assert.AreEqual(30, sortedTransactions[2].Amount);
        Assert.AreEqual(150, sortedTransactions[2].Balance);

        Assert.AreEqual(new DateTime(2024, 6, 2), sortedTransactions[3].Date);
        Assert.AreEqual(40, sortedTransactions[3].Amount);
        Assert.AreEqual(190, sortedTransactions[3].Balance);

        Assert.AreEqual(new DateTime(2024, 6, 2), sortedTransactions[4].Date);
        Assert.AreEqual(50, sortedTransactions[4].Amount);
        Assert.AreEqual(240, sortedTransactions[4].Balance);

        Assert.AreEqual(new DateTime(2024, 6, 3), sortedTransactions[5].Date);
        Assert.AreEqual(60, sortedTransactions[5].Amount);
        Assert.AreEqual(300, sortedTransactions[5].Balance);
    }

    /// <summary>
    /// Test sorting - some banks have zero amount transactions e.g for interest rate changes.
    /// Make sure sorting works with these.
    /// </summary>
    [TestMethod]
    public void TestSortWithZeroAmount()
    {
        List<Transaction> transactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 0, Balance = 100, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 30, Balance = 130, Account = "S1", Reference = "c" },
        ];

        var sortedTransactions = Transactions.Sort(transactions).ToArray();

        Assert.AreEqual(new DateTime(2024, 6, 1), sortedTransactions[0].Date);
        Assert.AreEqual(10, sortedTransactions[0].Amount);
        Assert.AreEqual(100, sortedTransactions[0].Balance);

        Assert.AreEqual(new DateTime(2024, 6, 1), sortedTransactions[1].Date);
        Assert.AreEqual(0, sortedTransactions[1].Amount);
        Assert.AreEqual(100, sortedTransactions[1].Balance);

        Assert.AreEqual(new DateTime(2024, 6, 2), sortedTransactions[2].Date);
        Assert.AreEqual(30, sortedTransactions[2].Amount);
        Assert.AreEqual(130, sortedTransactions[2].Balance);
    }    
}
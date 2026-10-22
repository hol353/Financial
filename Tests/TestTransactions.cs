using Finance;

namespace Tests;

[TestClass]
public class TestTransactions
{
    /// <summary>
    /// Test the simple case where transactions are imported when new transactions exactly match existing ones.
    /// </summary>
    [TestMethod]
    public void TestMergeIntoExistingTransactions()
    {
        Transactions transactions = new(
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "1", Balance = 100, Category = "c1" },
                new() { Date = new DateTime(2024, 3, 25), Amount = 20, Account = "xxxx", Reference = "2", Balance = 120, Category = "c2" },
                new() { Date = new DateTime(2024, 3, 29), Amount = 30, Account = "xxxx", Reference = "3", Balance = 150, Category = "c3" },
                new() { Date = new DateTime(2024, 4, 2), Amount = 40, Account = "xxxx", Reference = "4", Balance = 190, Category = "c4" }                
            ]);
            
        transactions.Import( 
            [
                new() { Date = new DateTime(2024, 3, 29), Amount = 30, Account = "xxxx", Reference = "3", Balance = 150 },
                new() { Date = new DateTime(2024, 4, 02), Amount = 40, Account = "xxxx", Reference = "4", Balance = 190 },
                new() { Date = new DateTime(2024, 4, 05), Amount = 50, Account = "xxxx", Reference = "5", Balance = 240 },
                new() { Date = new DateTime(2024, 4, 08), Amount = 60, Account = "xxxx", Reference = "6", Balance = 300 }                
            ]);

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "1", Balance = 100, Category = "c1" },
            new() { Date = new DateTime(2024, 3, 25), Amount = 20, Account = "xxxx", Reference = "2", Balance = 120, Category = "c2" },
            new() { Date = new DateTime(2024, 3, 29), Amount = 30, Account = "xxxx", Reference = "3", Balance = 150, Category = "c3" },
            new() { Date = new DateTime(2024, 4, 2), Amount = 40, Account = "xxxx", Reference = "4", Balance = 190, Category = "c4" },
            new() { Date = new DateTime(2024, 4, 5), Amount = 50, Account = "xxxx", Reference = "5", Balance = 240, Details = "??????" },
            new() { Date = new DateTime(2024, 4, 8), Amount = 60, Account = "xxxx", Reference = "6", Balance = 300, Details = "??????" }                
        ];

        Assert.IsTrue(expectedTransactions.SequenceEqual(transactions.All, TransactionEqualityComparer.Instance));
    }

    /// <summary>
    /// Test that imports into a specific account works when there are multiple accounts in the existing transations.
    /// </summary>
    [TestMethod]
    public void TestMergeIntoMultipleAccounts()
    {
        Transactions transactions = new( 
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "1", Balance = 10, Category = "x1" },
                new() { Date = new DateTime(2024, 3, 20), Amount = 110, Account = "yyyy", Reference = "5", Balance = 100, Category = "y1" },
                new() { Date = new DateTime(2024, 3, 25), Amount = 20, Account = "xxxx", Reference = "2", Balance = 30, Category = "x2" },
                new() { Date = new DateTime(2024, 3, 25), Amount = 120, Account = "yyyy", Reference = "6", Balance = 220, Category = "y2" }
            ]);
            
        transactions.Import( 
            [
                new() { Date = new DateTime(2024, 3, 29), Amount = 30, Account = "xxxx", Reference = "3", Balance = 60 },
                new() { Date = new DateTime(2024, 3, 30), Amount = 40, Account = "xxxx", Reference = "4", Balance = 100 },
            ]);

        transactions.Import( 
            [
                new() { Date = new DateTime(2024, 3, 29), Amount = 130, Account = "yyyy", Reference = "7", Balance = 350 },
                new() { Date = new DateTime(2024, 3, 30), Amount = 140, Account = "yyyy", Reference = "8", Balance = 490 },
            ]);            

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "1", Balance = 10, Category = "x1" },
            new() { Date = new DateTime(2024, 3, 20), Amount = 110, Account = "yyyy", Reference = "5", Balance = 100, Category = "y1" },
            new() { Date = new DateTime(2024, 3, 25), Amount = 20, Account = "xxxx", Reference = "2", Balance = 30, Category = "x2" },
            new() { Date = new DateTime(2024, 3, 25), Amount = 120, Account = "yyyy", Reference = "6", Balance = 220, Category = "y2" },
            new() { Date = new DateTime(2024, 3, 29), Amount = 30, Account = "xxxx", Reference = "3", Balance = 60, Details = "??????" },
            new() { Date = new DateTime(2024, 3, 29), Amount = 130, Account = "yyyy", Reference = "7", Balance = 350, Details = "??????" },
            new() { Date = new DateTime(2024, 3, 30), Amount = 40, Account = "xxxx", Reference = "4", Balance = 100, Details = "??????" },
            new() { Date = new DateTime(2024, 3, 30), Amount = 140, Account = "yyyy", Reference = "8", Balance = 490, Details = "??????" }                
        ];

        Assert.IsTrue(expectedTransactions.SequenceEqual(transactions.All, TransactionEqualityComparer.Instance));
    }

    /// <summary>
    /// Test that an import succeeds where transactions should be considered the same but the references are a bit different.
    /// Some banks modify the reference field some days after the original transaction.
    /// </summary>
    [TestMethod]
    public void TestMergeWhereTransactionsAreSameButDifferentReferences()
    {
        Transactions transactions = new( 
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "yyyy4 asdf", Balance = 100 },
                new() { Date = new DateTime(2024, 3, 21), Amount = 20, Account = "xxxx", Reference = "BPAY BA67347594575 TRC RATES", Balance = 120}         
            ]);

        transactions.Import( 
            [
                new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "yyyy4 asdf", Balance = 100 },
                new() { Date = new DateTime(2024, 3, 21), Amount = 20, Account = "xxxx", Reference = "INTERNET BPAY TRC RATES 5789540", Balance = 120 }         
            ]);

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 3, 20), Amount = 10, Account = "xxxx", Reference = "yyyy4 asdf", Balance = 100 },
            new() { Date = new DateTime(2024, 3, 21), Amount = 20, Account = "xxxx", Reference = "INTERNET BPAY TRC RATES 5789540", Balance = 120 }         
        ];
        Assert.IsTrue(expectedTransactions.SequenceEqual(transactions.All, TransactionEqualityComparer.Instance));
    }    

    /// <summary>
    /// Test that an import succeeds when dates of transactions change.
    /// </summary>
    [TestMethod]
    public void TestMergeWhereTransactionsHaveChangedDates()
    {
        Transactions transactions = new(  
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 20, Balance = 120, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 150, Account = "S1", Reference = "c", Category = "c3" }            
        ]);
        
        // New transactions don't have a category and the second and 3rd transactions have new dates.
        transactions.Import(
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2024, 6, 4), Amount = 20, Balance = 120, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2024, 6, 5), Amount = 30, Balance = 150, Account = "S1", Reference = "c" },
        ]);

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 4), Amount = 20, Balance = 120, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 5), Amount = 30, Balance = 150, Account = "S1", Reference = "c", Category = "c3" }            
        ];
        Assert.IsTrue(expectedTransactions.SequenceEqual(transactions.All, TransactionEqualityComparer.Instance));
    }        

    /// <summary>
    /// Test that a re-dated transaction that is beyond the date range of existing transactions is imported correctly.
    /// </summary>
    [TestMethod]
    public void TestMergeWhereTransactionsHaveChangedDatesPastEndExisting()
    {
        Transactions transactions = new(  
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 990, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 20, Balance = 1010, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 1040, Account = "S1", Reference = "c", Category = "c3" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 40, Balance = 1080, Account = "S1", Reference = "d", Category = "c4" },
            new() { Date = new DateTime(2024, 6, 13), Amount = 50, Balance = 1130, Account = "S1", Reference = "e", Category = "c5" }            
        ]);
        
        // New transactions don't have a category and the second existing transaction (2/6/2024) has a new date (9/6/2024)
        // Also note the new transactions have adjusted bank balances to match the new order. This is what the bank does.
        transactions.Import(
        [
            new() { Date = new DateTime(2024, 5, 30), Amount = 5,  Balance = 980, Account = "S1", Reference = "?" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 990, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 1020, Account = "S1", Reference = "c" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 40, Balance = 1060, Account = "S1", Reference = "d" },
            new() { Date = new DateTime(2024, 6, 9), Amount = 20, Balance = 1080, Account = "S1", Reference = "b" }, // redated transaction.
            new() { Date = new DateTime(2024, 6, 13), Amount = 50, Balance = 1130, Account = "S1", Reference = "e" },
        ]);

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 5, 30), Amount = 5,  Balance = 980, Account = "S1", Reference = "?", Details = "??????" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 990, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 30, Balance = 1020, Account = "S1", Reference = "c", Category = "c3" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 40, Balance = 1060, Account = "S1", Reference = "d", Category = "c4" },
            new() { Date = new DateTime(2024, 6, 9), Amount = 20, Balance = 1080, Account = "S1", Reference = "b", Category = "c2" }, // redated transaction.
            new() { Date = new DateTime(2024, 6, 13), Amount = 50, Balance = 1130, Account = "S1", Reference = "e", Category = "c5" },
        ];

        Assert.IsTrue(expectedTransactions.SequenceEqual(transactions.All, TransactionEqualityComparer.Instance));       
    }      

    /// <summary>
    /// Test import with split transactions.
    /// </summary>
    [TestMethod]
    public void TestSplit()
    {
        Transactions transactions = new( 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 111, Balance = 111, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 222, Balance = 333, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 1), Amount = -111, Account = "S1", Split = "Negate", },
            new() { Date = new DateTime(2024, 6, 1), Amount = 100, Account = "S1", Split = "Split", Category = "split1" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 11, Account = "S1", Split = "Split", Category = "split2" }
        ]);
        
        transactions.Import(
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 111, Balance = 111, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 222, Balance = 333, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 333, Balance = 666, Account = "S1", Reference = "c" }
        ]);

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 111, Balance = 111, Account = "S1", Reference = "a", Category = "c1" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 222, Balance = 333, Account = "S1", Reference = "b", Category = "c2" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 333, Balance = 666, Account = "S1", Reference = "c", Details = "??????" },
            new() { Date = new DateTime(2024, 6, 1), Amount = -111, Account = "S1", Split = "Negate", },
            new() { Date = new DateTime(2024, 6, 1), Amount = 100, Account = "S1", Split = "Split", Category = "split1" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 11, Account = "S1", Split = "Split", Category = "split2" },            
        ];

        Assert.IsTrue(expectedTransactions.SequenceEqual(transactions.All, TransactionEqualityComparer.Instance));
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

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 20, Balance = 120, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 30, Balance = 150, Account = "S1", Reference = "c" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 40, Balance = 190, Account = "S1", Reference = "d" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 50, Balance = 240, Account = "S1", Reference = "e" },
            new() { Date = new DateTime(2024, 6, 3), Amount = 60, Balance = 300, Account = "S1", Reference = "f" },
        ];
        Assert.IsTrue(expectedTransactions.SequenceEqual(sortedTransactions, TransactionEqualityComparer.Instance));
    }

    /// <summary>
    /// Test sorting where there are multiple splits and the transactions are in order.
    /// </summary>
    [TestMethod]
    public void TestSortWithMultipleSplits()
    {
        List<Transaction> transactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 20, Balance = 120, Account = "S1", Reference = "b", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = -20, Balance = 0, Account = "S1", Reference = "b", Split = "Negate", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "b", Split = "Split", Category = "x" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "b", Split = "Split", Category = "y" },

            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 130, Account = "S1", Reference = "c", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 20, Balance = 150, Account = "S1", Reference = "d", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = -20, Balance = 0, Account = "S1", Reference = "d", Split = "Negate", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "d", Split = "Split", Category = "x" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "d", Split = "Split", Category = "y" },

            new() { Date = new DateTime(2024, 6, 2), Amount = 30, Balance = 180, Account = "S1", Reference = "c" },
        ];

        var sortedTransactions = Transactions.Sort(transactions).ToArray();

        // The splits move to the end of the day which is ok for now.
        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 20, Balance = 120, Account = "S1", Reference = "b", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 130, Account = "S1", Reference = "c", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 20, Balance = 150, Account = "S1", Reference = "d", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = -20, Balance = 0, Account = "S1", Reference = "b", Split = "Negate", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "b", Split = "Split", Category = "x" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "b", Split = "Split", Category = "y" },
            new() { Date = new DateTime(2024, 6, 1), Amount = -20, Balance = 0, Account = "S1", Reference = "d", Split = "Negate", Category = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "d", Split = "Split", Category = "x" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 0, Account = "S1", Reference = "d", Split = "Split", Category = "y" },

            new() { Date = new DateTime(2024, 6, 2), Amount = 30, Balance = 180, Account = "S1", Reference = "c" },
        ];
        Assert.IsTrue(expectedTransactions.SequenceEqual(sortedTransactions, TransactionEqualityComparer.Instance));
    }    

    /// <summary>
    /// Test sorting - some banks don't have their transactions in order such that the balances make sense.
    /// </summary>
    [TestMethod]
    public void TestSort2()
    {
        List<Transaction> transactions = 
        [
            new() { Date = new DateTime(2025, 9, 10), Amount = 112.5, Balance = 35791.79, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2025, 9, 10), Amount = 112.5, Balance = 35904.29, Account = "S1", Reference = "c" },
            new() { Date = new DateTime(2025, 9, 10), Amount = 935, Balance = 35679.29, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 9, 22), Amount = 150, Balance = 36211.79, Account = "S1", Reference = "d" },
            new() { Date = new DateTime(2025, 9, 22), Amount = 157.5, Balance = 36061.79, Account = "S1", Reference = "e" },
            new() { Date = new DateTime(2025, 9, 22), Amount = -125, Balance = 36086.79, Account = "S1", Reference = "f" },
        ];

        var sortedTransactions = Transactions.Sort(transactions).ToArray();

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2025, 9, 10), Amount = 935, Balance = 35679.29, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 9, 10), Amount = 112.5, Balance = 35791.79, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2025, 9, 10), Amount = 112.5, Balance = 35904.29, Account = "S1", Reference = "c" },
            new() { Date = new DateTime(2025, 9, 22), Amount = 157.5, Balance = 36061.79, Account = "S1", Reference = "e" },
            new() { Date = new DateTime(2025, 9, 22), Amount = 150, Balance = 36211.79, Account = "S1", Reference = "d" },
            new() { Date = new DateTime(2025, 9, 22), Amount = -125, Balance = 36086.79, Account = "S1", Reference = "f" },
        ];

        Assert.IsTrue(expectedTransactions.SequenceEqual(sortedTransactions, TransactionEqualityComparer.Instance));
    }

    /// <summary>
    /// Test sorting where the initial transactions are already in order but there are 2 possible solutions.
    /// </summary>
    [TestMethod]
    public void TestSortWith2SolutionsButInitiallyInOrder()
    {
        List<Transaction> transactions = 
        [
            new() { Date = new DateTime(2025, 11, 26), Amount = 157.5, Balance = 28341.16, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 11, 28), Amount = 180, Balance = 28521.16, Account = "S1", Reference = "c" },
            new() { Date = new DateTime(2025, 11, 28), Amount = -180, Balance = 28341.16, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 11, 28), Amount = -210, Balance = 28131.16, Account = "S1", Reference = "d" },
        ];

        var sortedTransactions = Transactions.Sort(transactions).ToArray();

        Assert.IsTrue(transactions.SequenceEqual(sortedTransactions, TransactionEqualityComparer.Instance));
    }  

    /// <summary>
    /// Test sorting where the initial transactions are NOT in order but there are 2 possible solutions.
    /// </summary>
    [TestMethod]
    public void TestSortWith2SolutionsButNOTInitiallyInOrder()
    {
        List<Transaction> transactions = 
        [
            new() { Date = new DateTime(2025, 11, 26), Amount = 157.5, Balance = 28341.16, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 11, 28), Amount = -210, Balance = 28131.16, Account = "S1", Reference = "d" },
            new() { Date = new DateTime(2025, 11, 28), Amount = -180, Balance = 28341.16, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 11, 28), Amount = 180, Balance = 28521.16, Account = "S1", Reference = "c" },
        ];

        var sortedTransactions = Transactions.Sort(transactions).ToArray();

        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2025, 11, 26), Amount = 157.5, Balance = 28341.16, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 11, 28), Amount = 180, Balance = 28521.16, Account = "S1", Reference = "c" },
            new() { Date = new DateTime(2025, 11, 28), Amount = -180, Balance = 28341.16, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2025, 11, 28), Amount = -210, Balance = 28131.16, Account = "S1", Reference = "d" },
        ];
        Assert.IsTrue(expectedTransactions.SequenceEqual(sortedTransactions, TransactionEqualityComparer.Instance));
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
        
        Transaction[] expectedTransactions = 
        [
            new() { Date = new DateTime(2024, 6, 1), Amount = 10, Balance = 100, Account = "S1", Reference = "a" },
            new() { Date = new DateTime(2024, 6, 1), Amount = 0, Balance = 100, Account = "S1", Reference = "b" },
            new() { Date = new DateTime(2024, 6, 2), Amount = 30, Balance = 130, Account = "S1", Reference = "c" },
        ];

        Assert.IsTrue(expectedTransactions.SequenceEqual(sortedTransactions, TransactionEqualityComparer.Instance));
    }   
   
}
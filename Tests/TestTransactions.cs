using ImportTransactions;

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
        string transactionsFilePath = Path.Combine(Path.GetTempPath(), "transactions.csv");
        string importFilePath = Path.Combine(Path.GetTempPath(), "test.csv");
        
        File.WriteAllLines(transactionsFilePath, [
            "Date,Amount,Account,Reference,Balance",
            "20 Mar 24,500.00,xxxx,yyyy4,5001.30",
            "25 Mar 24,260.00,xxxx,yyyy3,6031.30",
            "29 Mar 24,-1980.00,xxxx,yyyy2,4051.30",
            "02 Apr 24,10000.00,xxxx,yyyy1,14051.30",
        ]);

        File.WriteAllLines(importFilePath, [
            "Date,Amount,Account Number,,Transaction Type,Transaction Details,Balance,Category,Merchant Name",
            "29 Mar 24,-1980.00,xxxx, ,TRANSFER DEBIT,yyyy2,4051.30,Internal transfers,",
            "02 Apr 24,10000.00,xxxx, ,INTER-BANK CREDIT,yyyy1,14051.30,Transfers in,",
            "05 Apr 24,512,xxxx, ,ABCD,zzzz1,6666.40,Transfers in,",
            "08 Apr 24,234.00,xxxx, ,EFGH,zzzz2,35001.30,Transfers in,",
        ]);

        var transactions = Csv.Read<Transaction>(transactionsFilePath);
        var importedTransactions = BankTransactionFile.Read([importFilePath]);

        var mergedTransactions = Transactions.Merge(transactions, importedTransactions).ToArray();
        if (mergedTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual("xxxx", mergedTransactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), mergedTransactions[0].Date);
        Assert.AreEqual(500.0, mergedTransactions[0].Amount);
        Assert.AreEqual("yyyy4", mergedTransactions[0].Reference);
        Assert.AreEqual(5001.3, mergedTransactions[0].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,25), mergedTransactions[1].Date);
        Assert.AreEqual(260.0, mergedTransactions[1].Amount);
        Assert.AreEqual("yyyy3", mergedTransactions[1].Reference);
        Assert.AreEqual(6031.3, mergedTransactions[1].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[2].Account);
        Assert.AreEqual(new DateTime(2024,3,29), mergedTransactions[2].Date);
        Assert.AreEqual(-1980.0, mergedTransactions[2].Amount);
        Assert.AreEqual("yyyy2", mergedTransactions[2].Reference);
        Assert.AreEqual(4051.3, mergedTransactions[2].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[3].Account);
        Assert.AreEqual(new DateTime(2024,4,2), mergedTransactions[3].Date);
        Assert.AreEqual(10000.0, mergedTransactions[3].Amount);
        Assert.AreEqual("yyyy1", mergedTransactions[3].Reference);
        Assert.AreEqual(14051.3, mergedTransactions[3].Balance);


        Assert.AreEqual("xxxx", mergedTransactions[4].Account);
        Assert.AreEqual(new DateTime(2024,4,5), mergedTransactions[4].Date);
        Assert.AreEqual(512.0, mergedTransactions[4].Amount);
        Assert.AreEqual("zzzz1", mergedTransactions[4].Reference);
        Assert.AreEqual(6666.4, mergedTransactions[4].Balance);

        Assert.AreEqual("xxxx", mergedTransactions[5].Account);
        Assert.AreEqual(new DateTime(2024,4,8), mergedTransactions[5].Date);
        Assert.AreEqual(234.0, mergedTransactions[5].Amount);
        Assert.AreEqual("zzzz2", mergedTransactions[5].Reference);
        Assert.AreEqual(35001.3, mergedTransactions[5].Balance);

        File.Delete(transactionsFilePath);
        File.Delete(importFilePath);
    }

    /// <summary>
    /// Test the case where transactions should be considered the same but the references are a bit different.
    /// Some backs modify the reference field some days after the original transaction.
    /// </summary>
    [TestMethod]
    public void TestImportWhereTransactionsAreSameButDifferentReferences()
    {
        string transactionsFilePath = Path.Combine(Path.GetTempPath(), "transactions.csv");
        string importFilePath = Path.Combine(Path.GetTempPath(), "test.csv");
        
        File.WriteAllLines(transactionsFilePath, [
            "Date,Amount,Account,Reference,Balance",
            "20 Mar 24,500.00,xxxx,yyyy4 asdf,5001.30",
            "21 Mar 24,-1812.00,xxxx,BPAY BA67347594575 TRC RATES,2733.05",
        ]);

        // Try importing transaction again but with extra info appended to Reference field.
        File.WriteAllLines(importFilePath, [
            "Date,Amount,Account Number,,Transaction Type,Transaction Details,Balance,Category,Merchant Name",
            "20 Mar 24,500.00,xxxx, ,TRANSFER DEBIT,yyyy4 asdf Extra info,5001.30,Internal transfers,",
            "21 Mar 24,-1812.00,xxxx, ,TRANSFER DEBIT,INTERNET BPAY TRC RATES 5789540,9276.5,Internal transfers,",
        ]);

        var transactions = Csv.Read<Transaction>(transactionsFilePath);
        var importedTransactions = BankTransactionFile.Read([importFilePath]);

        var mergedTransactions = Transactions.Merge(transactions, importedTransactions).ToArray();
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

        File.Delete(transactionsFilePath);
        File.Delete(importFilePath);
    }    
}
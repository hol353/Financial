using System.Data;
using System.Reflection;
using System.Linq;
using ImportTransactions;

namespace Tests;

[TestClass]
public class TestTransactions
{
    [TestMethod]
    public void TestImportIntoEmptyTransactions()
    {
        string transactionsFilePath = Path.Combine(Path.GetTempPath(), "transactions.csv");
        string importFilePath = Path.Combine(Path.GetTempPath(), "test.csv");
        if (File.Exists(transactionsFilePath))
            File.Delete(transactionsFilePath);
        
        File.WriteAllLines(importFilePath, [
            "Date,Amount,Account Number,,Transaction Type,Transaction Details,Balance,Category,Merchant Name",
            "02 Apr 24,10000.00,xxxx, ,INTER-BANK CREDIT,yyyy1,14051.30,Transfers in,",
            "29 Mar 24,-1980.00,xxxx, ,TRANSFER DEBIT,yyyy2,4051.30,Internal transfers,",
            "25 Mar 24,260.00,xxxx, ,INTER-BANK CREDIT,yyyy3,6031.30,Transfers in,",
            "20 Mar 24,500.00,xxxx, ,INTER-BANK CREDIT,yyyy4,5001.30,Transfers in,",
        ]);

        Transactions transactions = new(transactionsFilePath);
        transactions.Import(importFilePath);
        transactions.Write();

        var actual = File.ReadAllText(transactionsFilePath);
        string expected =
            "Account,Date,Amount,Reference,Balance" + Environment.NewLine +
            "xxxx,2024-03-20,500,yyyy4,5001.3" + Environment.NewLine +
            "xxxx,2024-03-25,260,yyyy3,6031.3" + Environment.NewLine + 
            "xxxx,2024-03-29,-1980,yyyy2,4051.3" + Environment.NewLine + 
            "xxxx,2024-04-02,10000,yyyy1,14051.3" + Environment.NewLine;
        Assert.AreEqual(expected, actual);

        File.Delete(transactionsFilePath);
        File.Delete(importFilePath);
    }

    [TestMethod]
    public void TestImportIntoExistingTransactions()
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

        Transactions transactions = new(transactionsFilePath);
        transactions.Import(importFilePath);
        
        var allTransactions = transactions.AllTransactions?.ToArray();
        if (allTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual("xxxx", allTransactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), allTransactions[0].Date);
        Assert.AreEqual(500.0, allTransactions[0].Amount);
        Assert.AreEqual("yyyy4", allTransactions[0].Reference);
        Assert.AreEqual(5001.3, allTransactions[0].Balance);

        Assert.AreEqual("xxxx", allTransactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,25), allTransactions[1].Date);
        Assert.AreEqual(260.0, allTransactions[1].Amount);
        Assert.AreEqual("yyyy3", allTransactions[1].Reference);
        Assert.AreEqual(6031.3, allTransactions[1].Balance);

        Assert.AreEqual("xxxx", allTransactions[2].Account);
        Assert.AreEqual(new DateTime(2024,3,29), allTransactions[2].Date);
        Assert.AreEqual(-1980.0, allTransactions[2].Amount);
        Assert.AreEqual("yyyy2", allTransactions[2].Reference);
        Assert.AreEqual(4051.3, allTransactions[2].Balance);

        Assert.AreEqual("xxxx", allTransactions[3].Account);
        Assert.AreEqual(new DateTime(2024,4,2), allTransactions[3].Date);
        Assert.AreEqual(10000.0, allTransactions[3].Amount);
        Assert.AreEqual("yyyy1", allTransactions[3].Reference);
        Assert.AreEqual(14051.3, allTransactions[3].Balance);


        Assert.AreEqual("xxxx", allTransactions[4].Account);
        Assert.AreEqual(new DateTime(2024,4,5), allTransactions[4].Date);
        Assert.AreEqual(512.0, allTransactions[4].Amount);
        Assert.AreEqual("zzzz1", allTransactions[4].Reference);
        Assert.AreEqual(6666.4, allTransactions[4].Balance);

        Assert.AreEqual("xxxx", allTransactions[5].Account);
        Assert.AreEqual(new DateTime(2024,4,8), allTransactions[5].Date);
        Assert.AreEqual(234.0, allTransactions[5].Amount);
        Assert.AreEqual("zzzz2", allTransactions[5].Reference);
        Assert.AreEqual(35001.3, allTransactions[5].Balance);

        File.Delete(transactionsFilePath);
        File.Delete(importFilePath);
    }

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

        Transactions transactions = new(transactionsFilePath);
        transactions.Import(importFilePath);
        
        var allTransactions = transactions.AllTransactions?.ToArray();
        if (allTransactions == null)
            Assert.Fail("No transactions");

        Assert.AreEqual(2, allTransactions.Count());
        Assert.AreEqual("xxxx", allTransactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), allTransactions[0].Date);
        Assert.AreEqual(500.0, allTransactions[0].Amount);
        Assert.AreEqual("yyyy4 asdf", allTransactions[0].Reference); // transaction not changed via import.
        Assert.AreEqual(5001.3, allTransactions[0].Balance);

        Assert.AreEqual(2, allTransactions.Count());
        Assert.AreEqual("xxxx", allTransactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,21), allTransactions[1].Date);
        Assert.AreEqual(-1812.0, allTransactions[1].Amount);
        Assert.AreEqual("BPAY BA67347594575 TRC RATES", allTransactions[1].Reference); // transaction not changed via import.
        Assert.AreEqual(2733.05, allTransactions[1].Balance);

        File.Delete(transactionsFilePath);
        File.Delete(importFilePath);
    }    
}
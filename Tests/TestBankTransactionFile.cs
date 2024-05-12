using Finance;

namespace Tests;

[TestClass]
public class TestBankTransactionFile
{
    [TestMethod]
    public void TestReadCsvTransactions()
    {
        string importFilePath = Path.Combine(Path.GetTempPath(), "test.csv");
        
        File.WriteAllLines(importFilePath, [
            "Date,Amount,Account Number,,Transaction Type,Transaction Details,Balance,Category,Merchant Name",
            "02 Apr 24,10000.00,xxxx, ,INTER-BANK CREDIT,yyyy1,14051.30,Transfers in,",
            "29 Mar 24,-1980.00,xxxx, ,TRANSFER DEBIT,yyyy2,4051.30,Internal transfers,",
            "25 Mar 24,260.00,xxxx, ,INTER-BANK CREDIT,yyyy3,6031.30,Transfers in,",
            "20 Mar 24,500.00,xxxx, ,INTER-BANK CREDIT,yyyy4,5001.30,Transfers in,",
        ]);

        var transactions = BankTransactionFile.Read(importFilePath).ToList();

        Assert.AreEqual("xxxx", transactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,20), transactions[0].Date);
        Assert.AreEqual(500.0, transactions[0].Amount);
        Assert.AreEqual("yyyy4", transactions[0].Reference);
        Assert.AreEqual(5001.3, transactions[0].Balance);

        Assert.AreEqual("xxxx", transactions[1].Account);
        Assert.AreEqual(new DateTime(2024,3,25), transactions[1].Date);
        Assert.AreEqual(260.0, transactions[1].Amount);
        Assert.AreEqual("yyyy3", transactions[1].Reference);
        Assert.AreEqual(6031.3, transactions[1].Balance);

        Assert.AreEqual("xxxx", transactions[2].Account);
        Assert.AreEqual(new DateTime(2024,3,29), transactions[2].Date);
        Assert.AreEqual(-1980.0, transactions[2].Amount);
        Assert.AreEqual("yyyy2", transactions[2].Reference);
        Assert.AreEqual(4051.3, transactions[2].Balance);

        Assert.AreEqual("xxxx", transactions[3].Account);
        Assert.AreEqual(new DateTime(2024,4,2), transactions[3].Date);
        Assert.AreEqual(10000.0, transactions[3].Amount);
        Assert.AreEqual("yyyy1", transactions[3].Reference);
        Assert.AreEqual(14051.3, transactions[3].Balance);

        File.Delete(importFilePath);
    }


    [TestMethod]
    public void TestReadHeritageTransactions()
    {
        string importFilePath = Path.Combine(Path.GetTempPath(), "20240420_071656_2238578_S13_4NUQI35F.csv");

        File.WriteAllLines(importFilePath, [
            "20/04/2024,Balance,8,815.03,Current Balance for account S13",
            "20/04/2024,Balance,8,814.03,Available Balance for account S13",
            "Transaction Date, Amount, Reference, Balance",
            "\"31/03/2024\",\"0.22\",\"Interest credit\",\"8,815.03\""
        ]);

        var transactions = BankTransactionFile.Read(importFilePath).ToList();

        Assert.AreEqual("S13", transactions[0].Account);
        Assert.AreEqual(new DateTime(2024,3,31), transactions[0].Date);
        Assert.AreEqual(0.22, transactions[0].Amount);
        Assert.AreEqual("Interest credit", transactions[0].Reference);
        Assert.AreEqual(8815.03, transactions[0].Balance);

        File.Delete(importFilePath);
    }
}
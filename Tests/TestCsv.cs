using Finance;

namespace Tests;

[TestClass]
public class TestCsv
{
    class Test
    {
        public double A { get; set; }
        public double B { get; set; }
    }

    [TestMethod]
    public void TestCsvRead()
    {
        string filePath = Path.Combine(Path.GetTempPath(), "test.csv");
        
        File.WriteAllLines(filePath, [
            "A,B",
            "1,2",
            "3,4",
            "5,6"
        ]);

        var data = Csv.Read<Test>(filePath).ToArray();

        Assert.AreEqual(1.0, data[0].A);
        Assert.AreEqual(2.0, data[0].B);
        Assert.AreEqual(3.0, data[1].A);
        Assert.AreEqual(4.0, data[1].B);
        Assert.AreEqual(5.0, data[2].A);
        Assert.AreEqual(6.0, data[2].B);

        File.Delete(filePath);
    }

    [TestMethod]
    public void TestCsvReadWithMap()
    {
        string filePath = Path.Combine(Path.GetTempPath(), "test.csv");
        
        File.WriteAllLines(filePath, [
            "Y,B",
            "1,2",
            "3,4",
            "5,6"
        ]);

        var columnMap = new Dictionary<string, string> {  { "A", "Y" } };

        var data = Csv.Read<Test>(filePath, columnMap).ToArray();

        Assert.AreEqual(1.0, data[0].A);
        Assert.AreEqual(2.0, data[0].B);
        Assert.AreEqual(3.0, data[1].A);
        Assert.AreEqual(4.0, data[1].B);
        Assert.AreEqual(5.0, data[2].A);
        Assert.AreEqual(6.0, data[2].B);

        File.Delete(filePath);
    }    

    [TestMethod]
    public void TestCsvWrite()
    {
        Test[] data = [
            new() { A = 1, B = 2},
            new() { A = 3, B = 4}
        ];

        string fileName = Path.Combine(Path.GetTempPath(), "test.csv");
        
        Csv.Write(fileName, data);
       
        var lines = File.ReadAllLines(fileName);

        Assert.AreEqual("A,B", lines[0]);
        Assert.AreEqual("1,2", lines[1]);
        Assert.AreEqual("3,4", lines[2]);

        File.Delete(fileName);
    }    
}
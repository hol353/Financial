using System.Reflection;
using Finance;

namespace Tests;

[TestClass]
public class TestExcel
{
    class Test
    {
        public double A { get; set; }
        public double B { get; set; }
    }


    [TestMethod]
    public void TestExcelRead()
    {
        string fileName = Path.Combine(Path.GetTempPath(), "test.xlsx");

        using var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Tests.Resources.Test.xlsx");
        using var fileStream = File.Create(fileName);
        resourceStream?.Seek(0, SeekOrigin.Begin);
        resourceStream?.CopyTo(fileStream);
        resourceStream?.Close();
        fileStream.Close();
        var data = Excel.Read<Test>(fileName, "Sheet1").ToList();

        Assert.AreEqual(1.0, data[0].A);
        Assert.AreEqual(2.0, data[0].B);
        Assert.AreEqual(3.0, data[1].A);
        Assert.AreEqual(4.0, data[1].B);
        Assert.AreEqual(5.0, data[2].A);
        Assert.AreEqual(6.0, data[2].B);

        File.Delete(fileName);
    }

    [TestMethod]
    public void TestExcelWrite()
    {
        Test[] data = [
            new() { A = 1, B = 2},
            new() { A = 3, B = 4}
        ];
        string fileName = Path.Combine(Path.GetTempPath(), "test.xlsx");
        Excel.Write(fileName, "Test", data);
       
        data = Excel.Read<Test>(fileName, "Test").ToArray();

        Assert.AreEqual(1.0, data[0].A);
        Assert.AreEqual(2.0, data[0].B);
        Assert.AreEqual(3.0, data[1].A);
        Assert.AreEqual(4.0, data[1].B);

        File.Delete(fileName);
    }    
}
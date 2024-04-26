using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ImportTransactions;

public class Excel
{
    public static IEnumerable<T> Read<T>(string filePath, string sheetName) where T : new()
    {
        // Open the Excel file using ClosedXML.
        // Keep in mind the Excel file cannot be open when trying to read it
        using XLWorkbook workBook = new XLWorkbook(filePath);

        // Read the first Sheet from Excel file.
        IXLWorksheet workSheet = workBook.Worksheet(sheetName);

        // Create a new DataTable.
        List<T> data = [];

        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        // Loop through the Worksheet rows.
        List<string>? columnNames = null;
        foreach (IXLRow row in workSheet.Rows())
        {
            // Use the first row to add columns to DataTable.
            if (columnNames == null)
                columnNames = row.Cells().Select(c => c.GetValue<string>()).ToList();
            else
            {
                // Add rows to DataTable.
                T dataRow = new();
                int columnIndex = 1;

                foreach (var property in properties)
                {
                    IXLCell cell = row.Cell(columnIndex);

                    if (property.PropertyType == typeof(double))
                        property.SetValue(dataRow, cell.GetValue<double>());
                    else if (property.PropertyType == typeof(DateTime))
                        property.SetValue(dataRow, cell.GetValue<DateTime>());
                    else
                        property.SetValue(dataRow, cell.GetValue<string>());
                    columnIndex++;
                }

                data.Add(dataRow);
            }
        }

        return data;
    }

    public static void Write<T>(string filePath, string sheetName, IEnumerable<T> transactions)
    {
        XLWorkbook? workBook = null;
        try
        {
            if (File.Exists(filePath))
                workBook = new(filePath);
            else 
                workBook = new();

            if (!workBook.Worksheets.Any(w => w.Name == sheetName))
                workBook.Worksheets.Add(sheetName);

            IXLWorksheet workSheet = workBook.Worksheet(sheetName);

            var rows = workSheet.Rows();
            var cells = rows.Cells();
            IXLCell cell;
            if (cells.Any())
                cell = cells.First();
            else
                cell = workSheet.Cell("A1");
            cell.InsertTable(transactions);

            if (File.Exists(filePath))
                workBook.Save();
            else
                workBook.SaveAs(filePath);
        }
        finally
        {
            workBook?.Dispose();
        }
    }
}

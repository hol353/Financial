using ClosedXML.Excel;

namespace Finance;

public class Excel
{
    /// <summary>
    /// Read a flat table from an Excel spreadsheet.
    /// </summary>
    /// <typeparam name="T">The type to read the data into.</typeparam>
    /// <param name="filePath">The name and path of the file to read.</param>
    /// <param name="sheetName">The name of the worksheet to read.</param>
    /// <param name="startRowNumber">The starting row number to read from.</param>
    /// <returns>A collection of objects of type T.</returns>
    public static IEnumerable<T> Read<T>(string filePath, string sheetName, bool hasHeadings = true) where T : new()
    {
        // Open the Excel file using ClosedXML.
        // Keep in mind the Excel file cannot be open when trying to read it
        using XLWorkbook workBook = new XLWorkbook(filePath);

        // Read the first Sheet from Excel file.
        IXLWorksheet workSheet = workBook.Worksheet(sheetName);

        // Create a new DataTable.
        List<T> data = [];

        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
                                  .Where(p => p.CanWrite);

        // Loop through the Worksheet rows.
        foreach (IXLRow row in workSheet.Rows())
        {
            // Use the first row to add columns to DataTable.
            if (!hasHeadings)
            {
                // Add rows to DataTable.
                T dataRow = new();
                int columnIndex = 1;

                // For some reason workSheet.Rows can return blanks (when using LibreOffice Calc?).
                // When this happens, assume no more rows.
                if (row.Cell(1).GetValue<string>() == string.Empty)
                    break;

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
            hasHeadings = false;
        }

        return data;
    }

    /// <summary>
    /// Write a collection of objects to an Excel spreadsheet.
    /// </summary>
    /// <typeparam name="T">The type of the objects.</typeparam>
    /// <param name="filePath">The file name and path of the spreadsheet to write to.</param>
    /// <param name="sheetName">The worksheet to write to.</param>
    /// <param name="objects">The collection of objects to write.</param>
    public static void Write<T>(string filePath, string sheetName, IEnumerable<T> objects)
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
            cell = workSheet.Cell("A2");
            cell.InsertData(objects);

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

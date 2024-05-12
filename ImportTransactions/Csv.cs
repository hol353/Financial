using GenericParsing;

namespace Finance;

public class Csv
{
    /// <summary>
    /// Read a .csv file.
    /// </summary>
    /// <typeparam name="T">The type to read the data into.</typeparam>
    /// <param name="filePath">The file name and path to read data from.</param>
    /// <param name="columnMap">A column name map to convert from csv column headings to object properties</param>
    /// <returns>A collection of objects read.</returns>
    public static IEnumerable<T> Read<T>(string filePath, Dictionary<string, string>? columnMap = null) where T : new()
    {
        List<T> data = [];
        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        using GenericParserAdapter parser = new(filePath);
        parser.FirstRowHasHeader = true;
        parser.TrimResults = true;
        while (parser.Read())
        {
            T dataRow = new();
            foreach (var property in properties)
            {
                string propertyName = property.Name;
                if (columnMap != null && columnMap.TryGetValue(propertyName, out string? name))
                    propertyName = name;
                int columnIndex = parser.GetColumnIndex(propertyName);
                if (columnIndex != -1)
                {
                    if (property.PropertyType == typeof(double))
                        property.SetValue(dataRow, Convert.ToDouble(parser[columnIndex]));
                    else if (property.PropertyType == typeof(DateTime))
                        property.SetValue(dataRow, DateTime.Parse(parser[columnIndex]));
                    else
                        property.SetValue(dataRow, parser[columnIndex]);
                }
            }

            data.Add(dataRow);
        }
        if (data == null)
            throw new Exception($"Empty file: {filePath}");
        return data;
    }

    /// <summary>
    /// Write data to a .csv file.
    /// </summary>
    /// <typeparam name="T">The type of the object instances.</typeparam>
    /// <param name="filePath">The file name and path to write to.</param>
    /// <param name="data">The data to write.</param>
    /// <param name="dateFormat">The formmat to use when writing dates.</param>
    public static void Write<T>(string filePath, IEnumerable<T> data, string dateFormat="yyyy-MM-dd")
    {
        using StreamWriter writer = new(filePath); 

        // Write header row.
        var properties = typeof(T).GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        writer.WriteLine(string.Join(",", properties.Select(p => p.Name)));

        // Write all rows.
        foreach (var dataRow in data)
        {
            var values = properties.Select(p => 
            {
                var value = Convert.ChangeType(p.GetValue(dataRow), p.PropertyType);
                if (value == null)
                    return string.Empty;
                else if (p.PropertyType == typeof(DateTime))
                    return ((DateTime)value).ToString(dateFormat);
                else
                    return value.ToString();
            });
            writer.WriteLine(string.Join(",", values));
        }
        writer.Close();        
    }
}
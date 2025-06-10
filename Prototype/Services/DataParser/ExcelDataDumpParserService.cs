using System.Reflection;
using ClosedXML.Excel;
using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

/// <summary>
/// Parses Excel data dumps into model instances.
/// </summary>
public class ExcelDataDumpParserService : IDataDumpParserService
{
    public async Task<List<object>> ParseDataDump(ICollection<IFormFile> files, Type modelType)
    {
        var resultList = new List<object>();

        foreach (var file in files)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                var headerMap = BuildHeaderMap(worksheet.FirstRowUsed(), properties);

                foreach (var row in worksheet.RowsUsed().Skip(1))
                {
                    var instance = Activator.CreateInstance(modelType);

                    foreach (var (col, prop) in headerMap)
                    {
                        var cell = row.Cell(col);
                        if (cell.IsEmpty()) continue;

                        var value = ConvertCellValue(cell, prop.PropertyType, prop.Name);
                        if (value is SkipCell) continue; // Special marker for collections

                        prop.SetValue(instance, value);
                    }

                    resultList.Add(instance!);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse file '{file.FileName}': {ex.Message}", ex);
            }
        }

        return resultList;
    }

    private Dictionary<int, PropertyInfo> BuildHeaderMap(IXLRow headerRow, PropertyInfo[] properties)
    {
        var map = new Dictionary<int, PropertyInfo>();
        int colCount = headerRow.CellCount();
        for (int col = 1; col <= colCount; col++)
        {
            var header = headerRow.Cell(col).GetString();
            var prop = properties.FirstOrDefault(p => p.Name.Equals(header, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
                map[col] = prop;
        }
        return map;
    }

    private object? ConvertCellValue(IXLCell cell, Type propType, string propName)
    {
        try
        {
            if (propType == typeof(Guid))
                return Guid.Parse(cell.GetString());
            if (propType == typeof(Guid?))
                return string.IsNullOrEmpty(cell.GetString()) ? null : Guid.Parse(cell.GetString());
            if (propType == typeof(DateTime))
                return DateTime.Parse(cell.GetString());
            if (propType == typeof(long))
                return long.Parse(cell.GetString());
            if (propType.IsEnum)
                return System.Enum.Parse(propType, cell.GetString());
            if (propType == typeof(string))
                return cell.GetString();
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType) && propType != typeof(string))
                return SkipCell.Instance; // special marker for skipping collections
            return Convert.ChangeType(cell.Value, propType);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to convert value '{cell.Value}' in column '{propName}' to type '{propType.Name}': {ex.Message}", ex);
        }
    }

    private class SkipCell
    {
        public static readonly SkipCell Instance = new SkipCell();
    }
}
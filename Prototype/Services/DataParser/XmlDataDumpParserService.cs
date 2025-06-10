using System.Collections;
using System.Reflection;
using System.Xml.Linq;
using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

/// <summary>
/// Parses XML data dumps into model instances, supporting nested collections.
/// </summary>
public class XmlDataDumpParserService : IDataDumpParserService
{
    public async Task<List<object>> ParseDataDump(ICollection<IFormFile> files, Type modelType)
    {
        var resultList = new List<object>();

        foreach (var file in files)
        {
            try
            {
                await using var stream = file.OpenReadStream();
                var doc = XDocument.Load(stream);

                foreach (var element in doc.Root.Elements())
                {
                    var instance = Activator.CreateInstance(modelType);

                    foreach (var prop in modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var child = element.Element(prop.Name);
                        if (child == null) continue;

                        var value = ConvertXmlElement(child, prop.PropertyType, prop.Name);
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

    private object? ConvertXmlElement(XElement element, Type propType, string propName)
    {
        // Handle collections
        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(propType)
            && propType.IsGenericType
            && propType.GetGenericTypeDefinition() == typeof(ICollection<>))
        {
            var itemType = propType.GetGenericArguments()[0];
            var listType = typeof(List<>).MakeGenericType(itemType);
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var subElem in element.Elements())
            {
                var itemInstance = Activator.CreateInstance(itemType);
                foreach (var itemProp in itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var itemChild = subElem.Element(itemProp.Name);
                    if (itemChild == null) continue;
                    var itemValue = ConvertXmlElement(itemChild, itemProp.PropertyType, itemProp.Name);
                    itemProp.SetValue(itemInstance, itemValue);
                }
                list.Add(itemInstance!);
            }
            return list;
        }

        var valueStr = element.Value;
        try
        {
            if (propType == typeof(Guid))
                return Guid.Parse(valueStr);
            if (propType == typeof(Guid?))
                return string.IsNullOrEmpty(valueStr) ? null : Guid.Parse(valueStr);
            if (propType == typeof(DateTime))
                return DateTime.Parse(valueStr);
            if (propType == typeof(long))
                return long.Parse(valueStr);
            if (propType.IsEnum)
                return System.Enum.Parse(propType, valueStr);
            if (propType == typeof(string))
                return valueStr;
            return Convert.ChangeType(valueStr, propType);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to convert value '{valueStr}' for property '{propName}' to type '{propType.Name}': {ex.Message}", ex);
        }
    }
}
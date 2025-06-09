using Prototype.Data.Parser;
using Prototype.Enum;

namespace Prototype.Services;

public class DataDumpParserFactory(IServiceProvider serviceProvider)
{
    public IDataDumpParserService GetParser(DataDumpParseTypeEnum parseType)
    {
        return parseType switch
        {
            DataDumpParseTypeEnum.CSV => serviceProvider.GetRequiredService<CsvDataDumpParserService>(),
            DataDumpParseTypeEnum.JSON => serviceProvider.GetRequiredService<JsonDataDumpParserService>(),
            DataDumpParseTypeEnum.XML => serviceProvider.GetRequiredService<XmlDataDumpParserService>(),
            DataDumpParseTypeEnum.YAML => serviceProvider.GetRequiredService<YamlDataDumpParserService>(),
            DataDumpParseTypeEnum.Excel => serviceProvider.GetRequiredService<ExcelDataDumpParserService>(),
            DataDumpParseTypeEnum.SQL => serviceProvider.GetRequiredService<SqlDataDumpParserService>(),
            _=> throw new NotSupportedException($"Format {parseType} not supported")
        };
    }
}
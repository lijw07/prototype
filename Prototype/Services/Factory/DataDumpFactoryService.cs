using Prototype.Enum;
using Prototype.Services.DataParser;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class DataDumpParserFactoryService(IServiceProvider serviceProvider)
{
    public IDataDumpParserService GetParser(DataDumpParseTypeEnum parseType)
    {
        return parseType switch
        {
            DataDumpParseTypeEnum.CSV => serviceProvider.GetRequiredService<CsvDataDumpParserService>(),
            DataDumpParseTypeEnum.JSON => serviceProvider.GetRequiredService<JsonDataDumpParserService>(),
            DataDumpParseTypeEnum.XML => serviceProvider.GetRequiredService<XmlDataDumpParserService>(),
            DataDumpParseTypeEnum.Excel => serviceProvider.GetRequiredService<ExcelDataDumpParserService>(),
            _=> throw new NotSupportedException($"Format {parseType} not supported")
        };
    }
}
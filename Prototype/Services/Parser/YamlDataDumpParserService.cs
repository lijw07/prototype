using Prototype.DTOs;
using Prototype.Services;

namespace Prototype.Data.Parser;

public class YamlDataDumpParserService : IDataDumpParserService
{
    public Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(IFormFile file)
    {
        throw new NotImplementedException();
    }
}
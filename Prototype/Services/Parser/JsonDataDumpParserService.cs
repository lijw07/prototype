using Prototype.DTOs;
using Prototype.Services;

namespace Prototype.Data.Parser;

public class JsonDataDumpParserService : IDataDumpParserService
{
    public Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(IFormFile file)
    {
        
    }
}
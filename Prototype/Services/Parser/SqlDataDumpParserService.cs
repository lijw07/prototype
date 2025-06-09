using Prototype.DTOs;
using Prototype.Services;

namespace Prototype.Data.Parser;

public class SqlDataDumpParserService : IDataDumpParserService
{
    public Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(IFormFile file)
    {
        throw new NotImplementedException();
    }
}
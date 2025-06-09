using Prototype.DTOs;
using Prototype.Services.Interfaces;

namespace Prototype.Services.DataParser;

public class YamlDataDumpParserService : IDataDumpParserService
{
    public Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(ICollection<IFormFile> file)
    {
        throw new NotImplementedException();
    }
}
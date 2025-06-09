using Prototype.DTOs;

namespace Prototype.Services;

public interface IDataDumpParserService
{
    Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(IFormFile file);

}
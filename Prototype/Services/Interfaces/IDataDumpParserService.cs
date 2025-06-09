using Prototype.DTOs;

namespace Prototype.Services.Interfaces;

public interface IDataDumpParserService
{
    Task<List<TableSchemaDto>> ParseAndInferSchemasAsync(ICollection<IFormFile> file);
}
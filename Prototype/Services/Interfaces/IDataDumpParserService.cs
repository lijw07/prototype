namespace Prototype.Services.Interfaces;

public interface IDataDumpParserService
{
    Task<List<object>> ParseDataDump(ICollection<IFormFile> files, Type modelType);
}
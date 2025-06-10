namespace Prototype.Services.DataParser
{
    public abstract class BaseDataDumpParserService
    {
        protected async Task<List<object>> ParseFilesWithExceptionHandling(
            ICollection<IFormFile> files,
            Func<IFormFile, Task<IEnumerable<object>>> parserFunc)
        {
            var resultList = new List<object>();
            foreach (var file in files)
            {
                try
                {
                    var records = await parserFunc(file);
                    resultList.AddRange(records);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to parse file '{file.FileName}': {ex.Message}", ex);
                }
            }
            return resultList;
        }
    }
}
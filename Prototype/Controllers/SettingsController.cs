using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Services;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class SettingsController(
    DataDumpParserFactoryService parserFactoryService,
    SentinelContext context) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadDataDump([FromForm] DataDumpRequestDto requestDto)
    {
        var parser = parserFactoryService.GetParser(requestDto.DataDumpParseType);

        foreach (var formFile in requestDto.File)
        {
            var modelType = GetModelTypeFromFileName(formFile.FileName);
            if (modelType == null)
                return BadRequest($"Model type for {formFile.FileName} not found.");

            try
            {
                var entities = await parser.ParseDataDump(new List<IFormFile> { formFile }, modelType);

                var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(modelType))!;
                foreach (var entity in entities)
                    typedList.Add(entity);

                await SaveEntitiesToDatabase(modelType, typedList);
            }
            catch (Exception ex)
            {
                return UnprocessableEntity($"Failed to process file '{formFile.FileName}'.\nStack Trace: {ex.Message}");
            }
        }
        return Ok("Upload and processing successful.");
    }

    private async Task SaveEntitiesToDatabase(Type modelType, IList typedList)
    {
        var dbSet = context.GetType().GetMethod("Set", Type.EmptyTypes)!
            .MakeGenericMethod(modelType).Invoke(context, null);

        var addRangeMethod = dbSet!.GetType().GetMethod("AddRange", new[] { typeof(IEnumerable<>).MakeGenericType(modelType) });
        addRangeMethod!.Invoke(dbSet, new object[] { typedList });

        await context.SaveChangesAsync();
    }

    private static Type? GetModelTypeFromFileName(string fileName)
    {
        var modelName = Path.GetFileNameWithoutExtension(fileName);
        var modelType = Type.GetType($"Prototype.Models.{modelName}");
        return modelType;
    }
}
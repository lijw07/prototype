using System.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Services;
using Prototype.Services.Interfaces;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Prototype.Models;

namespace Prototype.Controllers.Settings;

[ApiController]
[Authorize]
[Route("[controller]")]
public class DataDumpSettingsController(
    DataDumpParserFactoryService parserFactoryService,
    IEntityCreationFactoryService entityCreationFactory,
    SentinelContext context)
    : ControllerBase
{
    private const string ModelNamespace = "Prototype.Models";

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDataDump([FromForm] DataDumpRequestDto requestDto)
    {
        var parser = parserFactoryService.GetParser(requestDto.DataDumpParseType);
        if (parser == null)
            return BadRequest("Parser not found for provided type.");

        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized("User not found.");
        
        var affectedTables = new List<string>();
        foreach (var formFile in requestDto.File)
        {
            var modelType = ResolveModelTypeFromFileName(formFile.FileName);
            if (modelType == null)
                return BadRequest($"Model type for '{formFile.FileName}' not found.");

            try
            {
                await ProcessFileAsync(formFile, parser, modelType);

                affectedTables.Add(modelType.Name);
                var activityLog = entityCreationFactory.CreateFromDataDump(user, HttpContext);
                var auditLog = entityCreationFactory.CreateFromDataDump(user, requestDto, affectedTables);

                await context.UserActivityLogs.AddAsync(activityLog);
                await context.AuditLogs.AddAsync(auditLog);
            }
            catch (Exception ex)
            {
                return UnprocessableEntity($"Failed to process file '{formFile.FileName}': {ex.Message}");
            }
        }

        await context.SaveChangesAsync();
        return Ok("Upload and processing successful.");
    }

    private async Task ProcessFileAsync(
        IFormFile formFile,
        IDataDumpParserService parser,
        Type modelType)
    {
        var entities = await parser.ParseDataDump(new List<IFormFile> { formFile }, modelType);
        var typedList = CreateTypedList(modelType, entities);
        await SaveEntitiesToDatabaseAsync(modelType, typedList);
    }

    private static Type? ResolveModelTypeFromFileName(string fileName)
    {
        var modelName = Path.GetFileNameWithoutExtension(fileName);
        var qualifiedName = $"{ModelNamespace}.{modelName}, {typeof(DataDumpSettingsController).Assembly.FullName}";
        return Type.GetType(qualifiedName);
    }

    private static IList CreateTypedList(Type modelType, IEnumerable<object> entities)
    {
        var listType = typeof(List<>).MakeGenericType(modelType);
        var typedList = (IList)Activator.CreateInstance(listType)!;
        foreach (var entity in entities)
            typedList.Add(entity);
        return typedList;
    }

    private async Task SaveEntitiesToDatabaseAsync(Type modelType, IList typedList)
    {
        var dbSet = context.GetType().GetMethod("Set", Type.EmptyTypes)!
            .MakeGenericMethod(modelType).Invoke(context, null);

        var addRangeMethod = dbSet!.GetType().GetMethod("AddRange", new[] { typedList.GetType() });
        addRangeMethod!.Invoke(dbSet, new object[] { typedList });
    }

    private async Task<UserModel?> GetCurrentUserAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var userId)
            ? await context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            : null;
    }
}
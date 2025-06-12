using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Services.Factory;
using Prototype.Services.Interfaces;

namespace Prototype.Controllers.Settings;

[ApiController]
[Route("[controller]")]
[Authorize]
public class DataDumpSettingsController(
    DataDumpParserFactoryService parserFactoryService,
    IEntityCreationFactoryService entityCreationFactory,
    SentinelContext context) : ControllerBase
{
    private const string ModelNamespace = "Prototype.Models";

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDataDump([FromForm] DataDumpRequestDto requestDto)
    {
        var parser = parserFactoryService.GetParser(requestDto.DataDumpParseType);
        var user = await GetAuthenticatedUserAsync();
        if (user is null)
            return Unauthorized("Authenticated user not found.");

        var affectedTables = new List<string>();

        foreach (var file in requestDto.File)
        {
            var modelType = ResolveModelTypeFromFileName(file.FileName);
            if (modelType == null)
                return BadRequest($"Unknown model type for file '{file.FileName}'.");

            try
            {
                await ProcessAndSaveFileAsync(file, parser, modelType);
                affectedTables.Add(modelType.Name);
            }
            catch (Exception ex)
            {
                return UnprocessableEntity($"Failed to process '{file.FileName}': {ex.Message}");
            }
        }
        
        var activityLog = entityCreationFactory.CreateUserActivityLog(user, ActionTypeEnum.DataDumpUpload, HttpContext);
        var auditLog = entityCreationFactory.CreateAuditLog(user, ActionTypeEnum.DataDumpUpload, affectedTables);
        await context.UserActivityLogs.AddAsync(activityLog);
        await context.AuditLogs.AddAsync(auditLog);
        await context.SaveChangesAsync();

        return Ok("Upload and processing completed successfully.");
    }

    private async Task ProcessAndSaveFileAsync(
        IFormFile formFile,
        IDataDumpParserService parser,
        Type modelType)
    {
        var entities = await parser.ParseDataDump(new List<IFormFile> { formFile }, modelType);
        var typedList = CreateTypedList(modelType, entities);
        await SaveToDbContextAsync(modelType, typedList);
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

    private Task SaveToDbContextAsync(Type modelType, IList list)
    {
        var dbSet = context.GetType().GetMethod("Set", Type.EmptyTypes)!
            .MakeGenericMethod(modelType).Invoke(context, null);
        var addRangeMethod = dbSet!.GetType().GetMethod("AddRange", [list.GetType()]);
        addRangeMethod!.Invoke(dbSet, [list]);
        return Task.CompletedTask;
    }

    private async Task<UserModel?> GetAuthenticatedUserAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userIdStr, out var userId)
            ? await context.Users.FirstOrDefaultAsync(u => u.UserId == userId)
            : null;
    }
}
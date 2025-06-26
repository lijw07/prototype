using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Enum;
using Prototype.Models;

namespace Prototype.Controllers.BulkUpload;

public abstract class BaseBulkUploadController : ControllerBase
{
    protected readonly SentinelContext Context;
    protected readonly ILogger Logger;

    protected BaseBulkUploadController(SentinelContext context, ILogger logger)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected async Task<UserModel?> GetCurrentUserAsync()
    {
        // Temporary: Return admin user for testing when authorization is disabled
        return await Context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    }

    protected async Task<byte[]> ReadFileDataAsync(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    protected async Task LogBulkUploadActivity(Guid userId, string tableType, int recordCount, bool success)
    {
        var activityLog = new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = userId,
            User = null, // Don't set navigation property to avoid tracking issues
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
            ActionType = success ? ActionTypeEnum.Import : ActionTypeEnum.ErrorLogged,
            Description = $"Bulk upload to {tableType} table - {recordCount} records - {(success ? "Success" : "Failed")}",
            Timestamp = DateTime.UtcNow
        };

        Context.UserActivityLogs.Add(activityLog);
        await Context.SaveChangesAsync();
    }

    protected async Task LogMultipleBulkUploadActivity(Guid userId, MultipleBulkUploadResponseDto responseDto)
    {
        var activityLog = new UserActivityLogModel
        {
            UserActivityLogId = Guid.NewGuid(),
            UserId = userId,
            User = null, // Don't set navigation property to avoid tracking issues
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            DeviceInformation = HttpContext.Request.Headers.UserAgent.ToString() ?? "Unknown",
            ActionType = responseDto.OverallSuccess ? ActionTypeEnum.Import : ActionTypeEnum.ErrorLogged,
            Description = $"Multiple file bulk upload - {responseDto.TotalFiles} files - Processed: {responseDto.ProcessedFiles} Failed: {responseDto.FailedFiles}",
            Timestamp = DateTime.UtcNow
        };

        Context.UserActivityLogs.Add(activityLog);
        await Context.SaveChangesAsync();
    }

    protected static string[] GetAllowedExtensions()
    {
        return new[] { ".csv", ".xml", ".json", ".xlsx", ".xls" };
    }

    protected bool ValidateFileExtension(string fileName, out string fileExtension)
    {
        fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowedExtensions = GetAllowedExtensions();
        return allowedExtensions.Contains(fileExtension);
    }
}
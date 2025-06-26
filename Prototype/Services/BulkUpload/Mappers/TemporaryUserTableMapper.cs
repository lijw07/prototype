using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prototype.DTOs.BulkUpload;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Services.BulkUpload.Mappers;

public class TemporaryUserTableMapper(
    IServiceScopeFactory scopeFactory,
    SentinelContext context,
    ILogger<TemporaryUserTableMapper> logger)
    : IBatchTableMapper
{
    public string TableType => "TemporaryUsers";

    public async Task<ValidationResultDto> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResultDto { IsValid = true };

        try
        {
            var firstName = GetColumnValue(row, "FirstName");
            var lastName = GetColumnValue(row, "LastName");
            var email = GetColumnValue(row, "Email");
            var requestedApplications = GetColumnValue(row, "RequestedApplications");
            
            if (string.IsNullOrWhiteSpace(firstName))
                result.Errors.Add($"Row {rowNumber}: FirstName is required");

            if (string.IsNullOrWhiteSpace(lastName))
                result.Errors.Add($"Row {rowNumber}: LastName is required");

            if (string.IsNullOrWhiteSpace(email))
                result.Errors.Add($"Row {rowNumber}: Email is required");
            
            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                result.Errors.Add($"Row {rowNumber}: Invalid email format");

            if (!string.IsNullOrWhiteSpace(firstName) && firstName.Length > 50)
                result.Errors.Add($"Row {rowNumber}: FirstName cannot exceed 50 characters");

            if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length > 50)
                result.Errors.Add($"Row {rowNumber}: LastName cannot exceed 50 characters");
            
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();
            
            if (!string.IsNullOrWhiteSpace(requestedApplications))
            {
                var appNames = requestedApplications.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(name => name.Trim())
                    .ToList();

                foreach (var appName in appNames)
                {
                    var appExists = await context.Applications
                        .AnyAsync(a => a.ApplicationName == appName, cancellationToken);
                    if (!appExists)
                        result.Errors.Add($"Row {rowNumber}: Application '{appName}' does not exist");
                }
            }
            
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingTempUser = await context.TemporaryUsers
                    .FirstOrDefaultAsync(tu => tu.Email == email, cancellationToken);
                if (existingTempUser != null)
                    result.Errors.Add($"Row {rowNumber}: Email '{email}' already has a temporary user request");

                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
                if (existingUser != null)
                    result.Errors.Add($"Row {rowNumber}: Email '{email}' already exists as an active user");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating temporary user row {RowNumber}", rowNumber);
            result.Errors.Add($"Row {rowNumber}: Validation error - {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    public Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var tempUser = new TemporaryUserModel
            {
                TemporaryUserId = Guid.NewGuid(),
                FirstName = GetColumnValue(row, "FirstName"),
                LastName = GetColumnValue(row, "LastName"),
                Email = GetColumnValue(row, "Email"),
                Username = GetColumnValue(row, "Email"), // Use email as username for now
                PasswordHash = "TEMP", // Temporary placeholder
                PhoneNumber = GetColumnValue(row, "PhoneNumber") ?? "N/A",
                CreatedAt = DateTime.UtcNow,
                Token = Guid.NewGuid().ToString()
            };

            context.TemporaryUsers.Add(tempUser);
            
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error saving temporary user row");
            return Task.FromResult(Result<bool>.Failure($"Error saving temporary user: {ex.Message}"));
        }
    }

    public List<TableColumnInfoDto> GetTemplateColumns()
    {
        return new List<TableColumnInfoDto>
        {
            new() { ColumnName = "FirstName", DataType = "string", IsRequired = true, MaxLength = 50, Description = "User's first name" },
            new() { ColumnName = "LastName", DataType = "string", IsRequired = true, MaxLength = 50, Description = "User's last name" },
            new() { ColumnName = "Email", DataType = "string", IsRequired = true, MaxLength = 100, Description = "User's email address" },
            new() { ColumnName = "RequestedApplications", DataType = "string", IsRequired = false, MaxLength = 500, Description = "Comma-separated list of application names" },
            new() { ColumnName = "Justification", DataType = "string", IsRequired = false, MaxLength = 1000, Description = "Justification for the user request" },
            new() { ColumnName = "RequestedBy", DataType = "string", IsRequired = false, MaxLength = 50, Description = "Who is requesting this user" }
        };
    }

    public List<Dictionary<string, object>> GetExampleData()
    {
        return new List<Dictionary<string, object>>
        {
            new()
            {
                ["FirstName"] = "Michael",
                ["LastName"] = "Johnson",
                ["Email"] = "michael.johnson@company.com",
                ["RequestedApplications"] = "Employee Portal, Inventory Management",
                ["Justification"] = "New employee starting in Operations department",
                ["RequestedBy"] = "hr.manager"
            },
            new()
            {
                ["FirstName"] = "Sarah",
                ["LastName"] = "Wilson",
                ["Email"] = "sarah.wilson@company.com",
                ["RequestedApplications"] = "Employee Portal",
                ["Justification"] = "Contractor requiring basic access",
                ["RequestedBy"] = "project.manager"
            }
        };
    }

    private string GetColumnValue(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName))
            return string.Empty;

        var value = row[columnName];
        return value == DBNull.Value ? string.Empty : value?.ToString() ?? string.Empty;
    }

    private bool IsValidEmail(string email)
    {
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return emailRegex.IsMatch(email);
    }

    public async Task<Dictionary<int, ValidationResultDto>> ValidateBatchAsync(DataTable dataTable, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<int, ValidationResultDto>();
        
        try
        {
            var allEmails = dataTable.Rows.Cast<DataRow>()
                .Select(row => GetColumnValue(row, "Email"))
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var allApplicationNames = dataTable.Rows.Cast<DataRow>()
                .Select(row => GetColumnValue(row, "RequestedApplications"))
                .Where(apps => !string.IsNullOrWhiteSpace(apps))
                .SelectMany(apps => apps.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(app => app.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var existingTempUsersData = await context.TemporaryUsers
                .Where(tu => allEmails.Contains(tu.Email))
                .Select(tu => tu.Email.ToLower())
                .ToListAsync(cancellationToken);
            var existingTempUsers = new HashSet<string>(existingTempUsersData);

            var existingUsersData = await context.Users
                .Where(u => allEmails.Contains(u.Email))
                .Select(u => u.Email.ToLower())
                .ToListAsync(cancellationToken);
            var existingUsers = new HashSet<string>(existingUsersData);

            var validApplicationsData = await context.Applications
                .Where(a => allApplicationNames.Contains(a.ApplicationName))
                .Select(a => a.ApplicationName.ToLower())
                .ToListAsync(cancellationToken);
            var validApplications = new HashSet<string>(validApplicationsData);

            var emailsInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var rowNumber = 1;
            foreach (DataRow row in dataTable.Rows)
            {
                var result = new ValidationResultDto { IsValid = true };

                var firstName = GetColumnValue(row, "FirstName");
                var lastName = GetColumnValue(row, "LastName");
                var email = GetColumnValue(row, "Email");
                var requestedApps = GetColumnValue(row, "RequestedApplications");

                if (string.IsNullOrWhiteSpace(firstName))
                    result.Errors.Add($"Row {rowNumber}: FirstName is required");

                if (string.IsNullOrWhiteSpace(lastName))
                    result.Errors.Add($"Row {rowNumber}: LastName is required");

                if (string.IsNullOrWhiteSpace(email))
                {
                    result.Errors.Add($"Row {rowNumber}: Email is required");
                }
                else
                {
                    if (!IsValidEmail(email))
                        result.Errors.Add($"Row {rowNumber}: Invalid email format");

                    if (existingTempUsers.Contains(email.ToLower()))
                        result.Errors.Add($"Row {rowNumber}: Email '{email}' already has a temporary user request (DUPLICATE)");

                    if (existingUsers.Contains(email.ToLower()))
                        result.Errors.Add($"Row {rowNumber}: Email '{email}' already exists as an active user (DUPLICATE)");

                    if (emailsInBatch.Contains(email))
                        result.Errors.Add($"Row {rowNumber}: Email '{email}' appears multiple times in this batch");
                    else
                        emailsInBatch.Add(email);
                }

                if (!string.IsNullOrWhiteSpace(requestedApps))
                {
                    var apps = requestedApps.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(app => app.Trim()).ToList();

                    foreach (var appName in apps)
                    {
                        if (!validApplications.Contains(appName.ToLower()))
                            result.Errors.Add($"Row {rowNumber}: Application '{appName}' does not exist");
                    }
                }

                result.IsValid = !result.Errors.Any();
                results[rowNumber] = result;
                rowNumber++;
            }

            logger.LogInformation("Batch validation completed for {RowCount} rows. Valid: {ValidCount}, Invalid: {InvalidCount}",
                results.Count, results.Count(r => r.Value.IsValid), results.Count(r => !r.Value.IsValid));

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during batch validation");
            throw;
        }
    }

    public async Task<Result<int>> SaveBatchAsync(DataTable dataTable, Guid userId, Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var tempUsersToAdd = new List<TemporaryUserModel>();
            var rowNumber = 1;
            var successCount = 0;

            foreach (DataRow row in dataTable.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                // Skip invalid rows if not ignoring errors
                if (validationResults.ContainsKey(rowNumber) && !validationResults[rowNumber].IsValid && !ignoreErrors)
                {
                    rowNumber++;
                    continue;
                }

                var tempUser = new TemporaryUserModel
                {
                    TemporaryUserId = Guid.NewGuid(),
                    FirstName = GetColumnValue(row, "FirstName"),
                    LastName = GetColumnValue(row, "LastName"),
                    Email = GetColumnValue(row, "Email"),
                    Username = GetColumnValue(row, "Username"),
                    PasswordHash = GetColumnValue(row, "PasswordHash"),
                    PhoneNumber = GetColumnValue(row, "PhoneNumber"),
                    CreatedAt = DateTime.UtcNow,
                    Token = GetColumnValue(row, "Token")
                };

                tempUsersToAdd.Add(tempUser);
                successCount++;
                rowNumber++;
            }

            // Bulk add all temporary users at once
            if (tempUsersToAdd.Any())
            {
                context.TemporaryUsers.AddRange(tempUsersToAdd);
                logger.LogInformation("Added {TempUserCount} temporary users to context for bulk save", tempUsersToAdd.Count);
            }

            return Result<int>.Success(successCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during batch save");
            return Result<int>.Failure($"Batch save error: {ex.Message}");
        }
    }
}
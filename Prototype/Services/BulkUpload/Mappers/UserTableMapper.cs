using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;

namespace Prototype.Services.BulkUpload.Mappers;

public class UserTableMapper(
    IServiceScopeFactory scopeFactory,
    SentinelContext context,
    PasswordEncryptionService passwordEncryption,
    ILogger<UserTableMapper> logger)
    : ITableMapper, IBatchTableMapper
{
    public string TableType => "Users";

    public async Task<ValidationResultDto> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default)
    {
        var result = new ValidationResultDto { IsValid = true };

        try
        {
            var username = GetColumnValue(row, "Username");
            var email = GetColumnValue(row, "Email");
            var firstName = GetColumnValue(row, "FirstName");
            var lastName = GetColumnValue(row, "LastName");
            var role = GetColumnValue(row, "Role");

            if (string.IsNullOrWhiteSpace(username))
                result.Errors.Add($"Row {rowNumber}: Username is required");

            if (string.IsNullOrWhiteSpace(email))
                result.Errors.Add($"Row {rowNumber}: Email is required");

            if (string.IsNullOrWhiteSpace(firstName))
                result.Errors.Add($"Row {rowNumber}: FirstName is required");

            if (string.IsNullOrWhiteSpace(lastName))
                result.Errors.Add($"Row {rowNumber}: LastName is required");

            if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                result.Errors.Add($"Row {rowNumber}: Invalid email format");

            if (!string.IsNullOrWhiteSpace(username) && username.Length > 50)
                result.Errors.Add($"Row {rowNumber}: Username cannot exceed 50 characters");

            if (!string.IsNullOrWhiteSpace(role) && !IsValidRole(role))
                result.Errors.Add($"Row {rowNumber}: Invalid role. Must be Admin, User, or PlatformAdmin");

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();
            
            if (!string.IsNullOrWhiteSpace(username))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var existingUser = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
                if (existingUser != null)
                    result.Errors.Add($"Row {rowNumber}: Username '{username}' already exists");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var existingEmail = await context.Users
                    .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
                if (existingEmail != null)
                    result.Errors.Add($"Row {rowNumber}: Email '{email}' already exists");
            }

            result.IsValid = !result.Errors.Any();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating user row {RowNumber}", rowNumber);
            result.Errors.Add($"Row {rowNumber}: Validation error - {ex.Message}");
            result.IsValid = false;
        }

        return result;
    }

    public Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var tempPassword = GenerateTemporaryPassword();
            var hashedPassword = passwordEncryption.HashPassword(tempPassword);

            var user = new UserModel
            {
                UserId = Guid.NewGuid(),
                Username = GetColumnValue(row, "Username"),
                Email = GetColumnValue(row, "Email"),
                FirstName = GetColumnValue(row, "FirstName"),
                LastName = GetColumnValue(row, "LastName"),
                PhoneNumber = GetColumnValue(row, "PhoneNumber"),
                Role = GetColumnValue(row, "Role") ?? "User",
                IsActive = ParseBoolean(GetColumnValue(row, "IsActive"), true),
                PasswordHash = hashedPassword,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user row");
            return Task.FromResult(Result<bool>.Failure($"Error creating user: {ex.Message}"));
        }
    }

    public List<TableColumnInfoDto> GetTemplateColumns()
    {
        return new List<TableColumnInfoDto>
        {
            new() { ColumnName = "Username", DataType = "string", IsRequired = true, MaxLength = 50, Description = "Unique username for the user" },
            new() { ColumnName = "Email", DataType = "string", IsRequired = true, MaxLength = 100, Description = "User's email address" },
            new() { ColumnName = "FirstName", DataType = "string", IsRequired = true, MaxLength = 50, Description = "User's first name" },
            new() { ColumnName = "LastName", DataType = "string", IsRequired = true, MaxLength = 50, Description = "User's last name" },
            new() { ColumnName = "PhoneNumber", DataType = "string", IsRequired = false, MaxLength = 20, Description = "User's phone number" },
            new() { ColumnName = "Role", DataType = "string", IsRequired = false, DefaultValue = "User", Description = "User role: Admin, User, or PlatformAdmin" },
            new() { ColumnName = "IsActive", DataType = "boolean", IsRequired = false, DefaultValue = "true", Description = "Whether the user is active" },
            new() { ColumnName = "Department", DataType = "string", IsRequired = false, MaxLength = 100, Description = "User's department" }
        };
    }

    public List<Dictionary<string, object>> GetExampleData()
    {
        return new List<Dictionary<string, object>>
        {
            new()
            {
                ["Username"] = "john.doe",
                ["Email"] = "john.doe@company.com",
                ["FirstName"] = "John",
                ["LastName"] = "Doe",
                ["PhoneNumber"] = "555-0123",
                ["Role"] = "User",
                ["IsActive"] = true,
                ["Department"] = "Engineering"
            },
            new()
            {
                ["Username"] = "jane.smith",
                ["Email"] = "jane.smith@company.com",
                ["FirstName"] = "Jane",
                ["LastName"] = "Smith",
                ["PhoneNumber"] = "555-0124",
                ["Role"] = "Admin",
                ["IsActive"] = true,
                ["Department"] = "IT"
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

    private bool ParseBoolean(string value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value.ToLower() switch
        {
            "true" or "yes" or "1" or "y" => true,
            "false" or "no" or "0" or "n" => false,
            _ => defaultValue
        };
    }

    private bool IsValidEmail(string email)
    {
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        return emailRegex.IsMatch(email);
    }

    private bool IsValidRole(string role)
    {
        var validRoles = new[] { "Admin", "User", "PlatformAdmin" };
        return validRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Dictionary<int, ValidationResultDto>> ValidateBatchAsync(DataTable dataTable, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<int, ValidationResultDto>();
        var validationErrors = new Dictionary<int, List<string>>();
        
        try
        {
            // Pre-load all existing usernames and emails in bulk to avoid N+1 queries
            var allUsernames = dataTable.AsEnumerable()
                .Select(row => GetColumnValue(row, "Username"))
                .Where(username => !string.IsNullOrWhiteSpace(username))
                .Distinct()
                .ToList();
            
            var allEmails = dataTable.AsEnumerable()
                .Select(row => GetColumnValue(row, "Email"))
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct()
                .ToList();

            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();
            
            // Bulk load existing usernames and emails
            var existingUsernamesData = await context.Users
                .Where(u => allUsernames.Contains(u.Username))
                .Select(u => u.Username)
                .ToListAsync(cancellationToken);
            var existingUsernames = new HashSet<string>(existingUsernamesData);
                
            var existingEmailsData = await context.Users
                .Where(u => allEmails.Contains(u.Email))
                .Select(u => u.Email)
                .ToListAsync(cancellationToken);
            var existingEmails = new HashSet<string>(existingEmailsData);

            // Validate each row using the pre-loaded data
            int rowNumber = 1;
            foreach (DataRow row in dataTable.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var errors = new List<string>();
                
                var username = GetColumnValue(row, "Username");
                var email = GetColumnValue(row, "Email");
                var firstName = GetColumnValue(row, "FirstName");
                var lastName = GetColumnValue(row, "LastName");
                var role = GetColumnValue(row, "Role");

                // Basic field validation
                if (string.IsNullOrWhiteSpace(username))
                    errors.Add($"Row {rowNumber}: Username is required");

                if (string.IsNullOrWhiteSpace(email))
                    errors.Add($"Row {rowNumber}: Email is required");

                if (string.IsNullOrWhiteSpace(firstName))
                    errors.Add($"Row {rowNumber}: FirstName is required");

                if (string.IsNullOrWhiteSpace(lastName))
                    errors.Add($"Row {rowNumber}: LastName is required");

                if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                    errors.Add($"Row {rowNumber}: Invalid email format");

                if (!string.IsNullOrWhiteSpace(username) && username.Length > 50)
                    errors.Add($"Row {rowNumber}: Username cannot exceed 50 characters");

                if (!string.IsNullOrWhiteSpace(role) && !IsValidRole(role))
                    errors.Add($"Row {rowNumber}: Invalid role. Must be Admin, User, or PlatformAdmin");

                // Check for duplicates using pre-loaded data (no database queries)
                if (!string.IsNullOrWhiteSpace(username) && existingUsernames.Contains(username))
                    errors.Add($"Row {rowNumber}: Username '{username}' already exists");

                if (!string.IsNullOrWhiteSpace(email) && existingEmails.Contains(email))
                    errors.Add($"Row {rowNumber}: Email '{email}' already exists");

                results[rowNumber] = new ValidationResultDto 
                { 
                    IsValid = !errors.Any(),
                    Errors = errors
                };
                
                rowNumber++;
            }
            
            logger.LogInformation("Batch validation completed for {TotalRows} rows. Valid: {ValidCount}, Invalid: {InvalidCount}", 
                dataTable.Rows.Count, results.Count(r => r.Value.IsValid), results.Count(r => !r.Value.IsValid));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during batch validation");
            throw;
        }

        return results;
    }

    public async Task<Result<int>> SaveBatchAsync(DataTable dataTable, Guid userId, Dictionary<int, ValidationResultDto> validationResults, bool ignoreErrors = false, CancellationToken cancellationToken = default)
    {
        try
        {
            var usersToAdd = new List<UserModel>();
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

                var tempPassword = GenerateTemporaryPassword();
                var hashedPassword = passwordEncryption.HashPassword(tempPassword);

                var user = new UserModel
                {
                    UserId = Guid.NewGuid(),
                    Username = GetColumnValue(row, "Username"),
                    Email = GetColumnValue(row, "Email"),
                    FirstName = GetColumnValue(row, "FirstName"),
                    LastName = GetColumnValue(row, "LastName"),
                    PhoneNumber = GetColumnValue(row, "PhoneNumber"),
                    Role = GetColumnValue(row, "Role") ?? "User",
                    IsActive = ParseBoolean(GetColumnValue(row, "IsActive"), true),
                    PasswordHash = hashedPassword,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                usersToAdd.Add(user);
                successCount++;
                rowNumber++;
            }

            // Bulk add all users at once
            if (usersToAdd.Any())
            {
                context.Users.AddRange(usersToAdd);
                logger.LogInformation("Added {UserCount} users to context for bulk save", usersToAdd.Count);
            }

            return Result<int>.Success(successCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during batch save");
            return Result<int>.Failure($"Batch save error: {ex.Message}");
        }
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
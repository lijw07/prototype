using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;

namespace Prototype.Services.BulkUpload.Mappers
{
    public class TemporaryUserTableMapper : ITableMapper
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SentinelContext _context;
        private readonly ILogger<TemporaryUserTableMapper> _logger;

        public string TableType => "TemporaryUsers";

        public TemporaryUserTableMapper(IServiceScopeFactory scopeFactory, SentinelContext context, ILogger<TemporaryUserTableMapper> logger)
        {
            _scopeFactory = scopeFactory;
            _context = context;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                var firstName = GetColumnValue(row, "FirstName");
                var lastName = GetColumnValue(row, "LastName");
                var email = GetColumnValue(row, "Email");
                var requestedApplications = GetColumnValue(row, "RequestedApplications");

                // Required field validation
                if (string.IsNullOrWhiteSpace(firstName))
                    result.Errors.Add($"Row {rowNumber}: FirstName is required");

                if (string.IsNullOrWhiteSpace(lastName))
                    result.Errors.Add($"Row {rowNumber}: LastName is required");

                if (string.IsNullOrWhiteSpace(email))
                    result.Errors.Add($"Row {rowNumber}: Email is required");

                // Format validation
                if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                    result.Errors.Add($"Row {rowNumber}: Invalid email format");

                if (!string.IsNullOrWhiteSpace(firstName) && firstName.Length > 50)
                    result.Errors.Add($"Row {rowNumber}: FirstName cannot exceed 50 characters");

                if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length > 50)
                    result.Errors.Add($"Row {rowNumber}: LastName cannot exceed 50 characters");

                // Use fresh DbContext scope for validation queries
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();

                // Validate requested applications exist
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

                // Check for duplicates
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
                _logger.LogError(ex, "Error validating temporary user row {RowNumber}", rowNumber);
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

                _context.TemporaryUsers.Add(tempUser);
                // Note: SaveChanges will be called by the service after all rows are processed
                
                return Task.FromResult(Result<bool>.Success(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving temporary user row");
                return Task.FromResult(Result<bool>.Failure($"Error saving temporary user: {ex.Message}"));
            }
        }

        public List<TableColumnInfo> GetTemplateColumns()
        {
            return new List<TableColumnInfo>
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
    }
}
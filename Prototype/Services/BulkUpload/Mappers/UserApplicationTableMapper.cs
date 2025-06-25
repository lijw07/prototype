using System.Data;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;

namespace Prototype.Services.BulkUpload.Mappers
{
    public class UserApplicationTableMapper : ITableMapper
    {
        private readonly SentinelContext _context;
        private readonly ILogger<UserApplicationTableMapper> _logger;

        public string TableType => "UserApplications";

        public UserApplicationTableMapper(SentinelContext context, ILogger<UserApplicationTableMapper> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                var username = GetColumnValue(row, "Username");
                var applicationName = GetColumnValue(row, "ApplicationName");
                var permissionLevel = GetColumnValue(row, "PermissionLevel");
                var expirationDate = GetColumnValue(row, "ExpirationDate");

                // Required field validation
                if (string.IsNullOrWhiteSpace(username))
                    result.Errors.Add($"Row {rowNumber}: Username is required");

                if (string.IsNullOrWhiteSpace(applicationName))
                    result.Errors.Add($"Row {rowNumber}: ApplicationName is required");

                // Validate user exists
                if (!string.IsNullOrWhiteSpace(username))
                {
                    var userExists = await _context.Users
                        .AnyAsync(u => u.Username == username && u.IsActive);
                    if (!userExists)
                        result.Errors.Add($"Row {rowNumber}: User '{username}' does not exist or is inactive");
                }

                // Validate application exists
                if (!string.IsNullOrWhiteSpace(applicationName))
                {
                    var appExists = await _context.Applications
                        .AnyAsync(a => a.ApplicationName == applicationName);
                    if (!appExists)
                        result.Errors.Add($"Row {rowNumber}: Application '{applicationName}' does not exist");
                }

                // Validate permission level
                if (!string.IsNullOrWhiteSpace(permissionLevel) && !IsValidPermissionLevel(permissionLevel))
                    result.Errors.Add($"Row {rowNumber}: Invalid PermissionLevel. Must be Read, Write, or Admin");

                // Validate expiration date
                if (!string.IsNullOrWhiteSpace(expirationDate) && !DateTime.TryParse(expirationDate, out _))
                    result.Errors.Add($"Row {rowNumber}: Invalid ExpirationDate format");

                // Check for existing assignment
                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(applicationName))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var existingAssignment = await _context.UserApplications
                        .Include(ua => ua.User)
                        .Include(ua => ua.Application)
                        .FirstOrDefaultAsync(ua => 
                            ua.User.Username == username && 
                            ua.Application.ApplicationName == applicationName, cancellationToken);
                    
                    if (existingAssignment != null)
                        result.Errors.Add($"Row {rowNumber}: User '{username}' is already assigned to application '{applicationName}'");
                }

                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user application row {RowNumber}", rowNumber);
                result.Errors.Add($"Row {rowNumber}: Validation error - {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        public async Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check for cancellation before processing
                cancellationToken.ThrowIfCancellationRequested();
                
                var username = GetColumnValue(row, "Username");
                var applicationName = GetColumnValue(row, "ApplicationName");

                // Get user and application IDs
                cancellationToken.ThrowIfCancellationRequested();
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
                    
                cancellationToken.ThrowIfCancellationRequested();
                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.ApplicationName == applicationName, cancellationToken);

                if (user == null || application == null)
                {
                    return Result<bool>.Failure("User or Application not found");
                }

                var expirationDateStr = GetColumnValue(row, "ExpirationDate");
                DateTime? expirationDate = null;
                if (!string.IsNullOrWhiteSpace(expirationDateStr) && DateTime.TryParse(expirationDateStr, out var parsedDate))
                {
                    expirationDate = parsedDate;
                }

                // For now, we'll need to create a default application connection or require it to exist
                // This is a simplified implementation - in practice you might want to create a default connection
                cancellationToken.ThrowIfCancellationRequested();
                var defaultConnection = await _context.ApplicationConnections
                    .FirstOrDefaultAsync(ac => ac.ApplicationId == application.ApplicationId, cancellationToken);
                
                if (defaultConnection == null)
                {
                    return Result<bool>.Failure("No application connection found for this application");
                }

                var userApplication = new UserApplicationModel
                {
                    UserApplicationId = Guid.NewGuid(),
                    UserId = user.UserId,
                    User = user,
                    ApplicationId = application.ApplicationId,
                    Application = application,
                    ApplicationConnectionId = defaultConnection.ApplicationConnectionId,
                    ApplicationConnection = defaultConnection,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserApplications.Add(userApplication);
                // Note: SaveChanges will be called by the service after all rows are processed
                
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user application row");
                return Result<bool>.Failure($"Error saving user application assignment: {ex.Message}");
            }
        }

        public List<TableColumnInfo> GetTemplateColumns()
        {
            return new List<TableColumnInfo>
            {
                new() { ColumnName = "Username", DataType = "string", IsRequired = true, MaxLength = 50, Description = "Username of the user to assign" },
                new() { ColumnName = "ApplicationName", DataType = "string", IsRequired = true, MaxLength = 100, Description = "Name of the application to assign" },
                new() { ColumnName = "PermissionLevel", DataType = "string", IsRequired = false, DefaultValue = "Read", Description = "Permission level: Read, Write, or Admin" },
                new() { ColumnName = "ExpirationDate", DataType = "datetime", IsRequired = false, Description = "When the assignment expires (optional)" },
                new() { ColumnName = "Notes", DataType = "string", IsRequired = false, MaxLength = 500, Description = "Additional notes about the assignment" }
            };
        }

        public List<Dictionary<string, object>> GetExampleData()
        {
            return new List<Dictionary<string, object>>
            {
                new()
                {
                    ["Username"] = "john.doe",
                    ["ApplicationName"] = "Employee Portal",
                    ["PermissionLevel"] = "Read",
                    ["ExpirationDate"] = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd"),
                    ["Notes"] = "Standard employee access"
                },
                new()
                {
                    ["Username"] = "jane.smith",
                    ["ApplicationName"] = "Inventory Management",
                    ["PermissionLevel"] = "Write",
                    ["ExpirationDate"] = "",
                    ["Notes"] = "Operations team member"
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

        private bool IsValidPermissionLevel(string permissionLevel)
        {
            var validLevels = new[] { "Read", "Write", "Admin" };
            return validLevels.Contains(permissionLevel, StringComparer.OrdinalIgnoreCase);
        }
    }
}
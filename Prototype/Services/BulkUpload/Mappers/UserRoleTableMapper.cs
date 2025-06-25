using System.Data;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;

namespace Prototype.Services.BulkUpload.Mappers
{
    public class UserRoleTableMapper : IBatchTableMapper
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly SentinelContext _context;
        private readonly ILogger<UserRoleTableMapper> _logger;

        public string TableType => "UserRoles";

        public UserRoleTableMapper(IServiceScopeFactory scopeFactory, SentinelContext context, ILogger<UserRoleTableMapper> logger)
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
                var role = GetColumnValue(row, "Role");
                var createdBy = GetColumnValue(row, "CreatedBy");

                // Required field validation
                if (string.IsNullOrWhiteSpace(role))
                    result.Errors.Add($"Row {rowNumber}: Role is required");

                if (string.IsNullOrWhiteSpace(createdBy))
                    result.Errors.Add($"Row {rowNumber}: CreatedBy is required");

                // Format validation
                if (!string.IsNullOrWhiteSpace(role))
                {
                    if (role.Length > 100)
                        result.Errors.Add($"Row {rowNumber}: Role name cannot exceed 100 characters");

                    if (role.Trim() != role)
                        result.Errors.Add($"Row {rowNumber}: Role name cannot have leading or trailing whitespace");
                }

                if (!string.IsNullOrWhiteSpace(createdBy) && createdBy.Length > 50)
                    result.Errors.Add($"Row {rowNumber}: CreatedBy cannot exceed 50 characters");

                // Use fresh DbContext scope for validation queries
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();

                // Check for duplicates (case-insensitive)
                if (!string.IsNullOrWhiteSpace(role))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var existingRole = await context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.Role.ToLower() == role.ToLower(), cancellationToken);
                    if (existingRole != null)
                        result.Errors.Add($"Row {rowNumber}: Role '{role}' already exists");
                }

                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user role row {RowNumber}", rowNumber);
                result.Errors.Add($"Row {rowNumber}: Validation error - {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        public Task<Result<bool>> SaveRowAsync(DataRow row, Guid userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check for cancellation before processing
                cancellationToken.ThrowIfCancellationRequested();

                var userRole = new UserRoleModel
                {
                    UserRoleId = Guid.NewGuid(),
                    Role = GetColumnValue(row, "Role").Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = GetColumnValue(row, "CreatedBy")
                };

                _context.UserRoles.Add(userRole);
                // Note: SaveChanges will be called by the service after all rows are processed

                return Task.FromResult(Result<bool>.Success(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user role row");
                return Task.FromResult(Result<bool>.Failure($"Error creating user role: {ex.Message}"));
            }
        }

        public List<TableColumnInfo> GetTemplateColumns()
        {
            return new List<TableColumnInfo>
            {
                new() { ColumnName = "Role", DataType = "string", IsRequired = true, MaxLength = 100, Description = "Name of the user role (must be unique)" },
                new() { ColumnName = "CreatedBy", DataType = "string", IsRequired = true, MaxLength = 50, Description = "Username of who is creating this role" }
            };
        }

        public List<Dictionary<string, object>> GetExampleData()
        {
            return new List<Dictionary<string, object>>
            {
                new()
                {
                    ["Role"] = "Data Analyst",
                    ["CreatedBy"] = "system.admin"
                },
                new()
                {
                    ["Role"] = "Project Manager",
                    ["CreatedBy"] = "hr.manager"
                },
                new()
                {
                    ["Role"] = "Developer",
                    ["CreatedBy"] = "system.admin"
                },
                new()
                {
                    ["Role"] = "QA Tester",
                    ["CreatedBy"] = "team.lead"
                }
            };
        }

        public async Task<Dictionary<int, ValidationResult>> ValidateBatchAsync(DataTable dataTable, CancellationToken cancellationToken = default)
        {
            var results = new Dictionary<int, ValidationResult>();

            try
            {
                // Pre-load existing data for efficient lookups
                var allRoles = dataTable.Rows.Cast<DataRow>()
                    .Select(row => GetColumnValue(row, "Role"))
                    .Where(role => !string.IsNullOrWhiteSpace(role))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Batch database queries
                var existingRolesData = await _context.UserRoles
                    .Where(ur => allRoles.Contains(ur.Role))
                    .Select(ur => ur.Role.ToLower())
                    .ToListAsync(cancellationToken);
                var existingRoles = new HashSet<string>(existingRolesData);

                // Track duplicates within the batch
                var rolesInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Process each row
                var rowNumber = 1;
                foreach (DataRow row in dataTable.Rows)
                {
                    var result = new ValidationResult { IsValid = true };

                    var role = GetColumnValue(row, "Role");
                    var createdBy = GetColumnValue(row, "CreatedBy");

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(role))
                        result.Errors.Add($"Row {rowNumber}: Role is required");

                    if (string.IsNullOrWhiteSpace(createdBy))
                        result.Errors.Add($"Row {rowNumber}: CreatedBy is required");

                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        // Validate format
                        if (role.Length > 100)
                            result.Errors.Add($"Row {rowNumber}: Role name cannot exceed 100 characters");

                        if (role.Trim() != role)
                            result.Errors.Add($"Row {rowNumber}: Role name cannot have leading or trailing whitespace");

                        // Check for duplicates in existing data
                        if (existingRoles.Contains(role.ToLower()))
                            result.Errors.Add($"Row {rowNumber}: Role '{role}' already exists");

                        // Check for duplicates within batch
                        if (rolesInBatch.Contains(role))
                            result.Errors.Add($"Row {rowNumber}: Role '{role}' appears multiple times in this batch");
                        else
                            rolesInBatch.Add(role);
                    }

                    if (!string.IsNullOrWhiteSpace(createdBy) && createdBy.Length > 50)
                        result.Errors.Add($"Row {rowNumber}: CreatedBy cannot exceed 50 characters");

                    result.IsValid = !result.Errors.Any();
                    results[rowNumber] = result;
                    rowNumber++;
                }

                _logger.LogInformation("Batch validation completed for {RowCount} rows. Valid: {ValidCount}, Invalid: {InvalidCount}",
                    results.Count, results.Count(r => r.Value.IsValid), results.Count(r => !r.Value.IsValid));

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch validation");
                throw;
            }
        }

        public async Task<Result<int>> SaveBatchAsync(DataTable dataTable, Guid userId, Dictionary<int, ValidationResult> validationResults, bool ignoreErrors = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var rolesToAdd = new List<UserRoleModel>();
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

                    var userRole = new UserRoleModel
                    {
                        RoleId = Guid.NewGuid(),
                        Role = GetColumnValue(row, "Role"),
                        Description = GetColumnValue(row, "Description"),
                        Permissions = GetColumnValue(row, "Permissions"),
                        CreatedBy = GetColumnValue(row, "CreatedBy"),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    rolesToAdd.Add(userRole);
                    successCount++;
                    rowNumber++;
                }

                // Bulk add all roles at once
                if (rolesToAdd.Any())
                {
                    _context.UserRoles.AddRange(rolesToAdd);
                    _logger.LogInformation("Added {RoleCount} user roles to context for bulk save", rolesToAdd.Count);
                }

                return Result<int>.Success(successCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch save");
                return Result<int>.Failure($"Batch save error: {ex.Message}");
            }
        }

        private string GetColumnValue(DataRow row, string columnName)
        {
            if (!row.Table.Columns.Contains(columnName))
                return string.Empty;

            var value = row[columnName];
            return value == DBNull.Value ? string.Empty : value?.ToString() ?? string.Empty;
        }
    }
}
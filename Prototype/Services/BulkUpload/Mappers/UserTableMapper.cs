using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs.BulkUpload;
using Prototype.Helpers;
using Prototype.Models;

namespace Prototype.Services.BulkUpload.Mappers
{
    public class UserTableMapper : ITableMapper
    {
        private readonly SentinelContext _context;
        private readonly PasswordEncryptionService _passwordEncryption;
        private readonly ILogger<UserTableMapper> _logger;

        public string TableType => "Users";

        public UserTableMapper(
            SentinelContext context,
            PasswordEncryptionService passwordEncryption,
            ILogger<UserTableMapper> logger)
        {
            _context = context;
            _passwordEncryption = passwordEncryption;
            _logger = logger;
        }

        public async Task<ValidationResult> ValidateRowAsync(DataRow row, int rowNumber, CancellationToken cancellationToken = default)
        {
            var result = new ValidationResult { IsValid = true };

            try
            {
                var username = GetColumnValue(row, "Username");
                var email = GetColumnValue(row, "Email");
                var firstName = GetColumnValue(row, "FirstName");
                var lastName = GetColumnValue(row, "LastName");
                var role = GetColumnValue(row, "Role");

                // Required field validation
                if (string.IsNullOrWhiteSpace(username))
                    result.Errors.Add($"Row {rowNumber}: Username is required");

                if (string.IsNullOrWhiteSpace(email))
                    result.Errors.Add($"Row {rowNumber}: Email is required");

                if (string.IsNullOrWhiteSpace(firstName))
                    result.Errors.Add($"Row {rowNumber}: FirstName is required");

                if (string.IsNullOrWhiteSpace(lastName))
                    result.Errors.Add($"Row {rowNumber}: LastName is required");

                // Format validation
                if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                    result.Errors.Add($"Row {rowNumber}: Invalid email format");

                if (!string.IsNullOrWhiteSpace(username) && username.Length > 50)
                    result.Errors.Add($"Row {rowNumber}: Username cannot exceed 50 characters");

                if (!string.IsNullOrWhiteSpace(role) && !IsValidRole(role))
                    result.Errors.Add($"Row {rowNumber}: Invalid role. Must be Admin, User, or PlatformAdmin");

                // Check for duplicates
                if (!string.IsNullOrWhiteSpace(username))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var existingUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
                    if (existingUser != null)
                        result.Errors.Add($"Row {rowNumber}: Username '{username}' already exists");
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var existingEmail = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
                    if (existingEmail != null)
                        result.Errors.Add($"Row {rowNumber}: Email '{email}' already exists");
                }

                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user row {RowNumber}", rowNumber);
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
                
                // Generate temporary password
                var tempPassword = GenerateTemporaryPassword();
                var hashedPassword = _passwordEncryption.HashPassword(tempPassword);

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

                _context.Users.Add(user);
                // Note: SaveChanges will be called by the service after all rows are processed
                
                return Task.FromResult(Result<bool>.Success(true));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user row");
                return Task.FromResult(Result<bool>.Failure($"Error saving user: {ex.Message}"));
            }
        }

        public List<TableColumnInfo> GetTemplateColumns()
        {
            return new List<TableColumnInfo>
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

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
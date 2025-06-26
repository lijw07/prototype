using System.Data;
using Prototype.Common;
using Prototype.DTOs.BulkUpload;

namespace Prototype.Services.BulkUpload;

public interface IBulkValidationService
{
    Task<ValidationResult> ValidateDataTableAsync(DataTable dataTable, ValidationContext context);
    Task<ValidationResult> ValidateDataTableWithProgressAsync(
        DataTable dataTable, 
        ValidationContext context, 
        Func<double, Task> progressCallback);
}

public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    
    public static ValidationResult Success() => new() { IsSuccess = true };
    public static ValidationResult Failure(string message, List<string>? errors = null) => new()
    {
        IsSuccess = false,
        ErrorMessage = message,
        Errors = errors ?? new List<string>()
    };
}
using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs.BulkUpload;

public class BulkUploadRequestDto
{
    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;

    public bool IgnoreErrors { get; set; } = false;

    public Dictionary<string, string>? ColumnMappings { get; set; }
    
    public int? FileIndex { get; set; }
    public int? TotalFiles { get; set; }
    
    public string? JobId { get; set; }
}
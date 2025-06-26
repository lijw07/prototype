using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs.BulkUpload;

public class MultipleBulkUploadRequestDto
{
    [Required(ErrorMessage = "At least one file is required")]
    [MinLength(1, ErrorMessage = "At least one file must be provided")]
    public List<IFormFile> Files { get; set; } = new List<IFormFile>();

    public bool IgnoreErrors { get; set; } = false;

    public Dictionary<string, string>? ColumnMappings { get; set; }
        
    public bool ContinueOnError { get; set; } = true;
}
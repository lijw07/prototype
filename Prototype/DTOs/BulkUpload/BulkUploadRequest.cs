using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs.BulkUpload
{
    public class BulkUploadRequest
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; } = null!;

        public bool IgnoreErrors { get; set; } = false;

        public Dictionary<string, string>? ColumnMappings { get; set; }
        
        // Additional properties for multiple file support
        public int? FileIndex { get; set; }
        public int? TotalFiles { get; set; }
        
        // Job ID for SignalR progress tracking (optional, will be generated if not provided)
        public string? JobId { get; set; }
    }

    public class MultipleBulkUploadRequest
    {
        [Required(ErrorMessage = "At least one file is required")]
        [MinLength(1, ErrorMessage = "At least one file must be provided")]
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();

        public bool IgnoreErrors { get; set; } = false;

        public Dictionary<string, string>? ColumnMappings { get; set; }
        
        public bool ProcessFilesSequentially { get; set; } = true;
        
        public bool ContinueOnError { get; set; } = true;
    }
}
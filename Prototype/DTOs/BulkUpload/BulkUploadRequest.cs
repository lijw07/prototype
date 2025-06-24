using System.ComponentModel.DataAnnotations;

namespace Prototype.DTOs.BulkUpload
{
    public class BulkUploadRequest
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile File { get; set; } = null!;

        public bool SaveFile { get; set; } = false;

        public bool IgnoreErrors { get; set; } = false;

        public Dictionary<string, string>? ColumnMappings { get; set; }
    }
}
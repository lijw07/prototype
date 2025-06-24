using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models
{
    [Table("BulkUploadHistory")]
    public class BulkUploadHistoryModel
    {
        [Key]
        public Guid UploadId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TableType { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        public TimeSpan ProcessingTime { get; set; }

        public string? ErrorDetails { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; } = null!;
    }
}
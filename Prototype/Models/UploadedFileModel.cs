using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models
{
    [Table("UploadedFiles")]
    public class UploadedFileModel
    {
        [Key]
        public Guid FileId { get; set; }

        [Required]
        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TableType { get; set; } = string.Empty;

        [Required]
        public Guid UploadedBy { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime? DeletedAt { get; set; }

        public Guid? DeletedBy { get; set; }

        public int? RecordCount { get; set; }

        // Navigation properties
        [ForeignKey("UploadedBy")]
        public virtual UserModel UploadedByUser { get; set; } = null!;

        [ForeignKey("DeletedBy")]
        public virtual UserModel? DeletedByUser { get; set; }
    }
}
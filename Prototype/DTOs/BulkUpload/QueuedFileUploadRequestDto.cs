namespace Prototype.DTOs.BulkUpload;

public class QueuedFileUploadRequestDto
{
    public List<IFormFile> Files { get; set; } = new();
    public Guid UserId { get; set; }
    public bool IgnoreErrors { get; set; }
    public bool ContinueOnError { get; set; } = true;
}
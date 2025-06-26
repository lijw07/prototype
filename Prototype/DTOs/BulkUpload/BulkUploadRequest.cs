namespace Prototype.DTOs.BulkUpload;

/// <summary>
/// Encapsulates all bulk upload parameters to eliminate method parameter overload
/// </summary>
public class BulkUploadRequest
{
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string TableType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? FileName { get; set; }
    public bool IgnoreErrors { get; set; } = false;
    public CancellationToken CancellationToken { get; set; } = default;
}

public class BulkUploadWithProgressRequest : BulkUploadRequest
{
    public string JobId { get; set; } = string.Empty;
    public int FileIndex { get; set; } = 0;
    public int TotalFiles { get; set; } = 1;
}

public class MultipleBulkUploadRequest
{
    public List<BulkFileUpload> Files { get; set; } = new();
    public Guid UserId { get; set; }
    public bool IgnoreErrors { get; set; } = false;
    public CancellationToken CancellationToken { get; set; } = default;
}

public class BulkFileUpload
{
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string TableType { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
}

/// <summary>
/// Encapsulates validation context to reduce parameter passing
/// </summary>
public class ValidationContext
{
    public int RowNumber { get; set; }
    public string TableType { get; set; } = string.Empty;
    public List<string> ExistingUsernames { get; set; } = new();
    public List<string> ExistingEmails { get; set; } = new();
    public CancellationToken CancellationToken { get; set; } = default;
}

/// <summary>
/// Encapsulates file processing context
/// </summary>
public class FileProcessingContext
{
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int FileIndex { get; set; }
    public int TotalFiles { get; set; }
    public Guid UserId { get; set; }
    public bool IgnoreErrors { get; set; }
    public CancellationToken CancellationToken { get; set; } = default;
}
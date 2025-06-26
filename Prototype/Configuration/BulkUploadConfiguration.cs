namespace Prototype.Configuration;

public class BulkUploadConfiguration
{
    public const string SectionName = "BulkUpload";
    
    // File Processing Limits
    public int MaxFileSize { get; set; } = 50_000_000; // 50MB
    public int MaxRowThreshold { get; set; } = 50_000;
    public int LargeFileThreshold { get; set; } = 1_000;
    public string[] AllowedFileExtensions { get; set; } = { ".csv", ".xml", ".json", ".xlsx", ".xls" };
    
    // Performance Thresholds
    public int ProgressUpdateInterval { get; set; } = 100;
    public int ProgressLogInterval { get; set; } = 10; // Every 10 batches
    public int ValidationBatchSize { get; set; } = 1000;
    
    // Retry Configuration
    public int MaxRetries { get; set; } = 10;
    public int RetryDelaySeconds { get; set; } = 5;
    
    // Detection Thresholds
    public double MinDetectionConfidence { get; set; } = 0.3; // 30% confidence threshold
    public double HighConfidenceThreshold { get; set; } = 0.8; // 80% high confidence
    
    // Batch Processing
    public BatchSizeSettings BatchSizes { get; set; } = new();
    
    // Validation
    public ValidationConfiguration Validation { get; set; } = new();
    
    // Progress Tracking
    public ProgressConfiguration Progress { get; set; } = new();
}
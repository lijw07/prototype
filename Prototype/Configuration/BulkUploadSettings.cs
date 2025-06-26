namespace Prototype.Configuration;

public class BulkUploadSettings
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
    public ValidationSettings Validation { get; set; } = new();
    
    // Progress Tracking
    public ProgressSettings Progress { get; set; } = new();
    
    // Password Generation
    public PasswordGenerationSettings PasswordGeneration { get; set; } = new();
}

public class BatchSizeSettings
{
    public int SmallBatch { get; set; } = 500;    // < 1000 rows
    public int MediumBatch { get; set; } = 1000;  // < 5000 rows  
    public int LargeBatch { get; set; } = 2000;   // < 20000 rows
    public int XLargeBatch { get; set; } = 5000;  // >= 20000 rows
    
    public int GetBatchSize(int rowCount)
    {
        return rowCount switch
        {
            < 1000 => SmallBatch,
            < 5000 => MediumBatch,
            < 20000 => LargeBatch,
            _ => XLargeBatch
        };
    }
}

public class ValidationSettings
{
    public int MaxUsernameLength { get; set; } = 50;
    public int MaxEmailLength { get; set; } = 254;
    public int MaxFirstNameLength { get; set; } = 100;
    public int MaxLastNameLength { get; set; } = 100;
    public int MaxPhoneLength { get; set; } = 20;
    
    public string EmailRegexPattern { get; set; } = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public string[] ValidRoles { get; set; } = { "Admin", "User", "PlatformAdmin" };
}

public class ProgressSettings
{
    public int UpdateIntervalRows { get; set; } = 100;
    public int MinProgressUpdateIntervalMs { get; set; } = 500;
    public int MaxProgressUpdateIntervalMs { get; set; } = 2000;
}

public class PasswordGenerationSettings
{
    public int DefaultLength { get; set; } = 12;
    public string AllowedCharacters { get; set; } = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireSpecialChar { get; set; } = true;
}
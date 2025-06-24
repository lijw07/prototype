using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Helpers;
using Prototype.Models;

namespace Prototype.Services.BulkUpload
{
    public class FileStorageService : IFileStorageService
    {
        private readonly SentinelContext _context;
        private readonly ILogger<FileStorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _uploadPath;

        public FileStorageService(
            SentinelContext context,
            ILogger<FileStorageService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _uploadPath = _configuration["FileStorage:UploadPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            
            // Ensure upload directory exists
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<UploadedFileInfo> SaveFileAsync(IFormFile file, string tableType, Guid userId)
        {
            try
            {
                var fileId = Guid.NewGuid();
                var fileExtension = Path.GetExtension(file.FileName);
                var sanitizedFileName = SanitizeFileName(file.FileName);
                var uniqueFileName = $"{fileId}_{sanitizedFileName}";
                var filePath = Path.Combine(_uploadPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save file metadata to database
                var fileRecord = new UploadedFileModel
                {
                    FileId = fileId,
                    OriginalFileName = file.FileName,
                    StoredFileName = uniqueFileName,
                    FilePath = filePath,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    TableType = tableType,
                    UploadedBy = userId,
                    UploadedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.UploadedFiles.Add(fileRecord);
                await _context.SaveChangesAsync();

                return new UploadedFileInfo
                {
                    FileId = fileId,
                    FilePath = filePath,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    UploadedAt = fileRecord.UploadedAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file {FileName}", file.FileName);
                throw;
            }
        }

        public async Task<Result<byte[]>> GetFileAsync(Guid fileId, Guid userId)
        {
            try
            {
                var fileRecord = await _context.UploadedFiles
                    .FirstOrDefaultAsync(f => f.FileId == fileId && f.IsActive);

                if (fileRecord == null)
                {
                    return Result<byte[]>.Failure("File not found");
                }

                // Check if user has permission to access the file
                if (fileRecord.UploadedBy != userId)
                {
                    // Check if user is admin
                    var user = await _context.Users.FindAsync(userId);
                    if (user?.Role != "Admin")
                    {
                        return Result<byte[]>.Failure("Access denied");
                    }
                }

                if (!File.Exists(fileRecord.FilePath))
                {
                    return Result<byte[]>.Failure("File not found on disk");
                }

                var fileBytes = await File.ReadAllBytesAsync(fileRecord.FilePath);
                return Result<byte[]>.Success(fileBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file {FileId}", fileId);
                return Result<byte[]>.Failure($"Error retrieving file: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteFileAsync(Guid fileId, Guid userId)
        {
            try
            {
                var fileRecord = await _context.UploadedFiles
                    .FirstOrDefaultAsync(f => f.FileId == fileId && f.IsActive);

                if (fileRecord == null)
                {
                    return Result<bool>.Failure("File not found");
                }

                // Check if user has permission to delete the file
                if (fileRecord.UploadedBy != userId)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user?.Role != "Admin")
                    {
                        return Result<bool>.Failure("Access denied");
                    }
                }

                // Soft delete in database
                fileRecord.IsActive = false;
                fileRecord.DeletedAt = DateTime.UtcNow;
                fileRecord.DeletedBy = userId;

                // Optionally delete physical file
                if (_configuration.GetValue<bool>("FileStorage:DeletePhysicalFiles", false))
                {
                    if (File.Exists(fileRecord.FilePath))
                    {
                        File.Delete(fileRecord.FilePath);
                    }
                }

                await _context.SaveChangesAsync();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {FileId}", fileId);
                return Result<bool>.Failure($"Error deleting file: {ex.Message}");
            }
        }

        public async Task<List<StoredFileInfo>> GetUserFilesAsync(Guid userId)
        {
            try
            {
                var files = await _context.UploadedFiles
                    .Where(f => f.UploadedBy == userId && f.IsActive)
                    .OrderByDescending(f => f.UploadedAt)
                    .Select(f => new StoredFileInfo
                    {
                        FileId = f.FileId,
                        OriginalFileName = f.OriginalFileName,
                        TableType = f.TableType,
                        FileSize = f.FileSize,
                        UploadedAt = f.UploadedAt,
                        RecordCount = f.RecordCount ?? 0
                    })
                    .ToListAsync();

                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting files for user {UserId}", userId);
                throw;
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            
            // Limit length
            if (sanitized.Length > 100)
            {
                var extension = Path.GetExtension(sanitized);
                var nameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized);
                sanitized = nameWithoutExtension.Substring(0, 100 - extension.Length) + extension;
            }

            return sanitized;
        }
    }
}
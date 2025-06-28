using System.Data;

namespace Prototype.Services.BulkUpload;

public interface IFileParsingService
{
    DataTable? ParseFileToDataTable(byte[] fileData, string fileExtension);
}
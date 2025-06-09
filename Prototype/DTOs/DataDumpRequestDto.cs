using Prototype.Enum;

namespace Prototype.DTOs;

public class DataDumpRequestDto
{
    public required DataDumpParseTypeEnum DataDumpParseType { get; set; }
    public ICollection<IFormFile> File { get; set; }
}
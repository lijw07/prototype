using Prototype.Enum;

namespace Prototype.DTOs;

public class DataDumpRequestDto
{
    public required DataDumpParseTypeEnum DataDumpParseType { get; set; }
    public IFormFile File { get; set; }
}
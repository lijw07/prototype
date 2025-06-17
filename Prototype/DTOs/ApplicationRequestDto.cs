using Prototype.Enum;

namespace Prototype.DTOs;

public class ApplicationRequestDto
{
    public required string ApplicationName { get; set; }
    public required string ApplicationDescription { get; set; }
    public required DataSourceTypeEnum DataSourceType { get; set; }
    public required ConnectionSourceDto ConnectionSource { get; set; }
}
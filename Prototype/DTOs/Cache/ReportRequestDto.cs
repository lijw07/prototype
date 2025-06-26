namespace Prototype.DTOs.Cache;

public class ReportRequestDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Framework { get; set; } = "General";
    public string Format { get; set; } = "JSON";
    public bool IncludeAuditTrail { get; set; } = true;
    public bool IncludeUserActivity { get; set; } = true;
    public bool IncludeSecurityEvents { get; set; } = true;
    public bool IncludeViolations { get; set; } = true;
}
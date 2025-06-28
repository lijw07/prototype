using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers.Navigation;
using Prototype.Data;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("navigation/audit-log")]
public class AuditLogNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    TransactionService transactionService,
    IAuditLogService auditLogService,
    INavigationService navigationService,
    ILogger<AuditLogNavigationController> logger)
    : BaseNavigationController(logger, context, userAccessor, transactionService, auditLogService)
{
    private readonly INavigationService _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        return await ExecuteWithErrorHandlingAsync(async () =>
        {
            Logger.LogInformation("Getting audit logs - Page: {Page}, PageSize: {PageSize}", page, pageSize);
            
            var result = await _navigationService.GetAuditLogsPagedAsync(page, pageSize);
            return SuccessResponse(result, "Audit logs retrieved successfully");
        }, "retrieving audit logs");
    }
}
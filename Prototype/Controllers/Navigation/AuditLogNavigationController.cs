using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("[controller]")]
public class AuditLogNavigationController : BaseApiController
{
    private readonly INavigationService _navigationService;

    public AuditLogNavigationController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        TransactionService transactionService,
        IAuditLogService auditLogService,
        INavigationService navigationService,
        ILogger<AuditLogNavigationController> logger)
        : base(logger, context, userAccessor, transactionService, auditLogService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

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
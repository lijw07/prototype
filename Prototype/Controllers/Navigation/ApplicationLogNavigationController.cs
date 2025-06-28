using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("navigation/application-log")]
public class ApplicationLogNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    TransactionService transactionService,
    IAuditLogService auditLogService,
    INavigationService navigationService,
    ILogger<ApplicationLogNavigationController> logger)
    : BaseNavigationController(logger, context, userAccessor, transactionService, auditLogService)
{
    private readonly INavigationService _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        return await ExecuteWithErrorHandlingAsync(async () =>
        {
            var result = await _navigationService.GetApplicationLogsPagedAsync(page, pageSize);
            return SuccessResponse(result, "Application logs retrieved successfully");
        }, "retrieving application logs");
    }
}
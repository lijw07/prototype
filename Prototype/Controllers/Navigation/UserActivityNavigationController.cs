using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("navigation/user-activity")]
public class UserActivityNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    TransactionService transactionService,
    IAuditLogService auditLogService,
    INavigationService navigationService,
    ILogger<UserActivityNavigationController> logger)
    : BaseNavigationController(logger, context, userAccessor, transactionService, auditLogService)
{
    private readonly INavigationService _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        return await ExecuteWithErrorHandlingAsync(async () =>
        {
            var result = await _navigationService.GetUserActivityLogsPagedAsync(page, pageSize);
            return SuccessResponse(result, "User activity logs retrieved successfully");
        }, "retrieving user activity logs");
    }
}
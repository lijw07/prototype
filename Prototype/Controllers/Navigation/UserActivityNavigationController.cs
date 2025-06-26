using Microsoft.AspNetCore.Mvc;
using Prototype.Controllers;
using Prototype.Data;
using Prototype.Services;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Route("[controller]")]
public class UserActivityNavigationController : BaseApiController
{
    private readonly INavigationService _navigationService;

    public UserActivityNavigationController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        TransactionService transactionService,
        IAuditLogService auditLogService,
        INavigationService navigationService,
        ILogger<UserActivityNavigationController> logger)
        : base(logger, context, userAccessor, transactionService, auditLogService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

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
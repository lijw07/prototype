using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.DTOs;

namespace Prototype.Controllers.Dashboard;

[ApiController]
[Route("[controller]")]
public class DashboardController(
    SentinelContext context,
    HttpClient httpClient) : ControllerBase
{

    [HttpGet("users")]
    public async Task<IActionResult> GetUsersFromJsonPlaceholder()
    {
        var response = await httpClient.GetAsync("https://jsonplaceholder.typicode.com/users");

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Failed to fetch users from third-party API.");

        var stream = await response.Content.ReadAsStreamAsync();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var users = await JsonSerializer.DeserializeAsync<List<JsonPlaceholderUser>>(stream, options);

        return Ok(users);
    }
    
    [HttpPost("mock")]
    public async Task<IActionResult> CallMockPost()
    {
        var payload = new
        {
            name = "1234567890",
            job = "Fullstack Ghost"
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync("https://run.mocky.io/v3/2bbac42c-1775-4ca9-b4f9-45e364c72a9e", content);

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, "Mock API failed.");

        var result = await response.Content.ReadAsStringAsync();
        return Content(result, "application/json");
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await context.Employees.ToListAsync();
        return Ok(employees);
    }
    
    [HttpGet("activity-logs")]
    public async Task<IActionResult> GetActivityLogs()
    {
        var employees = await context.UserActivityLogs.ToListAsync();
        return Ok(employees);
    }
    
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var employees = await context.AuditLogs.ToListAsync();
        return Ok(employees);
    }
}
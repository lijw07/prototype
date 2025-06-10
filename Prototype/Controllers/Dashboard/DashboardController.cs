using Microsoft.AspNetCore.Mvc;
using Prototype.DTOs;

namespace Prototype.Controllers.Dashboard;

[ApiController]
[Route("[controller]")]
public class DashboardController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        throw new NotImplementedException();
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        throw new NotImplementedException();
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DashboardDto dto)
    {
        throw new NotImplementedException();
    }
    
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DashboardDto dto)
    {
        throw new NotImplementedException();
    }
}
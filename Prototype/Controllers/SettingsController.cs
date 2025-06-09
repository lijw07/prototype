using Microsoft.AspNetCore.Mvc;
using Prototype.Data;
using Prototype.DTOs;
using Prototype.Services;

namespace Prototype.Controllers;

[ApiController]
[Route("[controller]")]
public class SettingsController(
    DataDumpParserFactory parserFactory,
    SentinelContext context) : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadDataDump([FromForm] DataDumpRequestDto requestDto)
    {
        var parser = parserFactory.GetParser(requestDto.DataDumpParseType);
        var schemas = await parser.ParseAndInferSchemasAsync(requestDto.File);

        var tableNames = string.Join(", ", schemas.Select(s => s.TableName));
        return Ok(new { message = $"Processed {schemas.Count} tables: {tableNames}" });
    }
}
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

        var processedFiles = schemas.Select(s => s.TableName).ToList();
        return Ok(new { message = $"Processed {processedFiles.Count} files", files = processedFiles });
    }
}
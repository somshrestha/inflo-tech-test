using Microsoft.AspNetCore.Mvc;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogsService _auditLogsService;

    public AuditLogsController(IAuditLogsService auditLogsService)
    {
        _auditLogsService = auditLogsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(int page = 1, int pageSize = 10, string? search = null, string? actionType = null, bool sortDescending = true)
    {
        var (logs, total) = await _auditLogsService.GetAllAuditLogsAsync(page, pageSize, search, actionType, sortDescending);
        return Ok(new { Logs = logs, Total = total });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var log = await _auditLogsService.GetByIdAsync(id);
        if (log == null) throw new KeyNotFoundException($"User with ID {id} not found.");
        return Ok(log);
    }
}

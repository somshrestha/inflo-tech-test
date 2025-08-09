using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using UserManagement.Services.Domain.Interfaces;
using UserManagement.Web.Models.AuditLogs;

namespace UserManagement.Web.Controllers;

[Route("auditlogs")]
public class AuditLogsController : Controller
{
    private readonly IAuditLogsService _auditLogService;
    private readonly IMapper _mapper;
    public AuditLogsController(
        IAuditLogsService auditLogService,
        IMapper mapper)
    {
        _auditLogService = auditLogService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string? search = null, string? actionType = null, bool sortDescending = true)
    {
        try
        {
            var (logs, total) = await _auditLogService.GetAllAuditLogsAsync(page, pageSize, search, actionType, sortDescending);
            var model = new AuditLogListViewModel
            {
                Items = logs.Select(_mapper.Map<AuditLogViewModel>).ToList(),
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = total,
                SearchQuery = search,
                ActionTypeFilter = actionType,
                SortDescending = sortDescending
            };
            return View(model);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while retrieving audit logs.");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(long id)
    {
        try
        {
            var log = await _auditLogService.GetByIdAsync(id);
            if (log == null)
                return NotFound();

            var model = _mapper.Map<AuditLogViewModel>(log);
            return View(model);
        }
        catch (Exception)
        {
            return StatusCode(500, "An error occurred while retrieving the audit log.");
        }
    }
}

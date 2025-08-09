using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Entities;
using UserManagement.Services.Domain.Interfaces;

namespace UserManagement.Services.Domain.Domain.Implementations;
public class AuditLogsService : IAuditLogsService
{
    private readonly IDataContext _dataContext;
    public AuditLogsService(IDataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<(IEnumerable<AuditLog>, int)> GetAllAuditLogsAsync(int page, int pageSize, string? search, string? actionType, bool sortDescending = true)
    {
        var query = _dataContext.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(al => al.Details != null && al.Details.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            query = query.Where(al => al.ActionType == actionType);
        }

        query = sortDescending
            ? query.OrderByDescending(al => al.Timestamp)
            : query.OrderBy(al => al.Timestamp);

        var total = await query.CountAsync();
        var logs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs, total);
    }

    public async Task<AuditLog?> GetByIdAsync(long id)
    {
        return await _dataContext.GetByIdAsync<AuditLog>(id);
    }
}

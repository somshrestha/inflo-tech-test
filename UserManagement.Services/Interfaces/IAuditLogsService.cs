using UserManagement.Data.Entities;

namespace UserManagement.Services.Domain.Interfaces;
public interface IAuditLogsService
{
    /// <summary>
    /// Gets all the logs
    /// </summary>
    /// <returns></returns>
    Task<(IEnumerable<AuditLog>, int)> GetAllAuditLogsAsync(int page, int pageSize, string? search, string? actionType, bool sortDescending);

    /// <summary>
    /// Gets log by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<AuditLog?> GetByIdAsync(long id);
}

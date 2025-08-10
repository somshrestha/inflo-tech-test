namespace UserManagement.UI.Models;

public class AuditLogResponse
{
    public List<AuditLog>? Logs { get; set; }
    public int Total { get; set; }
}

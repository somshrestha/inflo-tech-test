namespace UserManagement.UI.Models;

public class AuditLog
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string? ActionType { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}

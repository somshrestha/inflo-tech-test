using System;

namespace UserManagement.Web.Models.AuditLogs;

public class AuditLogViewModel
{
    public long Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}

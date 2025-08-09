using System;

namespace UserManagement.Web.Models.AuditLogs;

public class AuditLogListViewModel
{
    public List<AuditLogViewModel> Items { get; set; } = new List<AuditLogViewModel>();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public string? SearchQuery { get; set; }
    public string? ActionTypeFilter { get; set; }
    public bool SortDescending { get; set; }
}

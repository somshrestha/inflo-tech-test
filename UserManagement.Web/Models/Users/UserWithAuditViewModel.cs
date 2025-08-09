using UserManagement.Web.Models.AuditLogs;

namespace UserManagement.Web.Models.Users;

public class UserWithAuditViewModel
{
    public UserViewModel User { get; set; } = new UserViewModel();
    public List<AuditLogViewModel> AuditLogs { get; set; } = new List<AuditLogViewModel>();
}

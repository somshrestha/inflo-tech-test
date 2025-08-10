using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UserManagement.Data.Entities;
using UserManagement.Models;

namespace UserManagement.Data.Interceptors;
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not DataContext context)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var auditLogs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<User>())
        {
            string actionType;
            string details;

            switch (entry.State)
            {
                case EntityState.Added:
                    actionType = "Create";
                    details = $"User {entry.Entity.Forename} {entry.Entity.Surname} created with email {entry.Entity.Email}";
                    break;
                case EntityState.Modified:
                    actionType = "Update";
                    var changes = GetChanges(entry);
                    details = changes.Any()
                        ? $"User {entry.Entity.Forename} {entry.Entity.Surname} updated: {string.Join(", ", changes)}"
                        : $"User {entry.Entity.Forename} {entry.Entity.Surname} updated with no changes detected";
                    break;
                case EntityState.Deleted:
                    actionType = "Delete";
                    details = $"User {entry.OriginalValues["Forename"]} {entry.OriginalValues["Surname"]} deleted with email {entry.OriginalValues["Email"]}";
                    break;
                default:
                    continue;
            }

            auditLogs.Add(new AuditLog
            {
                UserId = entry.Entity.Id,
                ActionType = actionType,
                Timestamp = DateTime.UtcNow,
                Details = details
            });
        }

        if (auditLogs.Any())
        {
            await context.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<string> GetChanges(EntityEntry<User> entry)
    {
        var changes = new List<string>();
        foreach (var property in entry.Properties)
        {
            var original = property.OriginalValue?.ToString() ?? "null";
            var current = property.CurrentValue?.ToString() ?? "null";

            if (property.IsModified && property.Metadata.Name != "Id" && original != current)
            {

                if (property.Metadata.Name == "DateOfBirth")
                {
                    original = original == "null" ? "null" : DateTime.Parse(original).ToString("yyyy-MM-dd");
                    current = current == "null" ? "null" : DateTime.Parse(current).ToString("yyyy-MM-dd");
                }

                changes.Add($"{property.Metadata.Name} changed from '{original}' to '{current}'");
            }
        }
        return changes;
    }
}

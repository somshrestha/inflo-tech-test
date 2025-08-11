using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Data.Interceptors;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        services.AddDbContext<DataContext>(options =>
        {
            options.UseSqlServer(connectionString);
            options.AddInterceptors(new AuditSaveChangesInterceptor());
        });

        services.AddScoped<IDataContext>(sp => sp.GetRequiredService<DataContext>());

        return services;
    }
}

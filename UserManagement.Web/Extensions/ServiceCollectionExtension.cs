using Microsoft.Extensions.DependencyInjection;
using UserManagement.Web.UserHelpers;

namespace UserManagement.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHelperServices(this IServiceCollection services)
        => services.AddScoped<IUserValidator, UserValidator>();
}

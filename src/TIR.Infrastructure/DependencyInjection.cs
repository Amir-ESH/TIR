using Microsoft.Extensions.DependencyInjection;
using TIR.Application.Employees;
using TIR.Infrastructure.Data;
using TIR.Infrastructure.Employees;

namespace TIR.Infrastructure;

/// <summary>
/// ثبت پیاده سازی های دسترسی به SQL در لایه Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("رشته اتصال دیتابیس Test تنظیم نشده است.", nameof(connectionString));

        services.AddSingleton(new SqlConnectionFactory(connectionString));
        services.AddScoped<IEmployeeReadRepository, EmployeeReadRepository>();
        return services;
    }
}

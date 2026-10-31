using Microsoft.Extensions.DependencyInjection;
using TIR.Application.Contracts;
using TIR.Application.Employees;
using TIR.Infrastructure.Attributes;
using TIR.Infrastructure.Bulk;
using TIR.Infrastructure.Data;
using TIR.Infrastructure.Employees;
using TIR.Infrastructure.Formulas;
using TIR.Infrastructure.Values;

namespace TIR.Infrastructure;

/// <summary>
/// ثبت پیاده سازی مخزن ها و اتصال SQL.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("رشته اتصال دیتابیس Test تنظیم نشده است.", nameof(connectionString));

        services.AddSingleton(new SqlConnectionFactory(connectionString));
        services.AddSingleton<StoredProcedureExecutor>();
        services.AddScoped<IEmployeeReadRepository, EmployeeReadRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IAttributeValueRepository, AttributeValueRepository>();
        services.AddScoped<IFormulaRepository, FormulaRepository>();
        services.AddScoped<IBulkRepository, BulkRepository>();
        return services;
    }
}

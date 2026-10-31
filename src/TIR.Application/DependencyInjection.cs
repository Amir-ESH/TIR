using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TIR.Application.Contracts;
using TIR.Application.Employees;

namespace TIR.Application;

/// <summary>ثبت سرویس ها و اعتبارسنجی های لایه Application.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<GetEmployeeQueryValidator>();
        services.AddScoped<IEmployeeQueryService, EmployeeQueryService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttributeService, AttributeService>();
        services.AddScoped<IAttributeValueService, AttributeValueService>();
        services.AddScoped<IFormulaService, FormulaService>();
        services.AddScoped<IBulkService, BulkService>();
        return services;
    }
}

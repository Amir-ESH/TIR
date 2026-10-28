using FluentValidation;

namespace TIR.Application.Employees;

/// <summary>
/// هماهنگی اعتبارسنجی ورودی و خواندن کارمند از مخزن.
/// </summary>
public sealed class EmployeeQueryService(IEmployeeReadRepository repository, 
    IValidator<GetEmployeeQuery> validator) : IEmployeeQueryService
{
    public async Task<EmployeeDetailsDto?> GetAsync(GetEmployeeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var employee = await repository.GetByIdAsync(request.Id, request.IncludeDeleted, cancellationToken);

        return employee is null
            ? null
            : EmployeeMapper.ToDetailsDto(employee);
    }
}

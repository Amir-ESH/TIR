namespace TIR.Application.Employees;

/// <summary>
/// قرارداد سناریوی خواندن اطلاعات کارمند.
/// </summary>
public interface IEmployeeQueryService
{
    Task<EmployeeDetailsDto?> GetAsync(GetEmployeeQuery request, CancellationToken cancellationToken);
}

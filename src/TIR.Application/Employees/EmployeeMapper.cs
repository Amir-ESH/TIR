using Riok.Mapperly.Abstractions;
using TIR.Domain.Employees;

namespace TIR.Application.Employees;

/// <summary>
/// تبدیل مدل دامنه به خروجی
/// Application توسط Mapperly.
/// </summary>
[Mapper]
public static partial class EmployeeMapper
{
    public static partial EmployeeDetailsDto ToDetailsDto(Employee employee);
}

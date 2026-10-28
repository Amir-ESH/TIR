using Riok.Mapperly.Abstractions;
using TIR.Domain.Employees;

namespace TIR.Infrastructure.Employees;

/// <summary>
/// تبدیل ردیف دریافت شده از SQL به مدل دامنه.
/// </summary>
[Mapper]
internal static partial class EmployeeDbMapper
{
    public static partial Employee ToDomain(EmployeeDbRow row);
}


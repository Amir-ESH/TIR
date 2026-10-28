using TIR.Domain.Employees;

namespace TIR.Application.Employees;

/// <summary>
/// قرارداد خواندن کارمند بدون وابستگی به فناوری پایگاه داده.
/// </summary>
public interface IEmployeeReadRepository
{
    Task<Employee?> GetByIdAsync(long id, bool includeDeleted, CancellationToken cancellationToken);
}

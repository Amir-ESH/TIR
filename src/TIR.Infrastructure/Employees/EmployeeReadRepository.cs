using TIR.Application.Employees;
using TIR.Domain.Employees;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Employees;

/// <summary>
/// خواندن کارمند فقط از اس پی.
/// </summary>
internal sealed class EmployeeReadRepository(StoredProcedureExecutor executor)
    : IEmployeeReadRepository
{
    public async Task<Employee?> GetByIdAsync(long id, bool includeDeleted, CancellationToken cancellationToken)
    {
        var row = await executor.SingleOrDefaultAsync<EmployeeDbRow>(
            "dbo.SP_Employee_Get", new { Id = id, IncludeDeleted = includeDeleted }, cancellationToken);

        return row is null
            ? null
            : EmployeeDbMapper.ToDomain(row);
    }
}

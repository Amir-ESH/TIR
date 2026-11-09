using System.Text.Json;
using TIR.Application.Contracts;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Employees;

/// <summary>
/// فقط ردیف های صفحه جاری را در یک فراخوانی به SQL Server می خواند.
/// </summary>
/// <param name="executor"></param>
internal sealed class EmployeeGridValueRepository(StoredProcedureExecutor executor) : IEmployeeGridValueRepository
{
    public Task<IReadOnlyList<EmployeeGridValueDto>> ReadAsync(IReadOnlyList<long> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
            return Task.FromResult<IReadOnlyList<EmployeeGridValueDto>>([]);

        return executor.ListAsync<EmployeeGridValueDto>("dbo.SP_Employee_GridValues",
            new { EmployeeIdsJson = JsonSerializer.Serialize(employeeIds) }, cancellationToken);
    }
}

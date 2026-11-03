using System.Data;
using System.Text.Json;
using Dapper;
using TIR.Application.Contracts;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Employees;

internal sealed class EmployeeExcelValueRepository(StoredProcedureExecutor executor) : IEmployeeExcelValueRepository
{
    public Task<IReadOnlyList<EmployeeFormAttributeDto>> ReadAsync(IReadOnlyList<long> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
            return Task.FromResult<IReadOnlyList<EmployeeFormAttributeDto>>([]);

        return executor.ListAsync<EmployeeFormAttributeDto>("dbo.SP_Employee_ExportValues",
            new { EmployeeIdsJson = JsonSerializer.Serialize(employeeIds) }, cancellationToken);
    }
}

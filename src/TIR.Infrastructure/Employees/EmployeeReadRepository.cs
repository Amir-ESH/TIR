using System.Data;
using Dapper;
using TIR.Application.Employees;
using TIR.Domain.Employees;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Employees;

/// <summary>
/// خواندن کارمند فقط از Stored Procedure و بدون SQL مستقیم.
/// </summary>
internal sealed class EmployeeReadRepository(SqlConnectionFactory connectionFactory)
    : IEmployeeReadRepository
{
    public async Task<Employee?> GetByIdAsync(long id, bool includeDeleted, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            commandText: "dbo.SP_Employee_Get",
            parameters: new { Id = id, IncludeDeleted = includeDeleted },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<EmployeeDbRow>(command);
        return row is null
            ? null
            : EmployeeDbMapper.ToDomain(row);
    }
}

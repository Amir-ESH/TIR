using TIR.Application.Contracts;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Values;

internal sealed class AttributeValueRepository(StoredProcedureExecutor executor) : IAttributeValueRepository
{
    public Task<EmployeeAttributeValueDto?> GetAsync(long employeeId, long attributeId, CancellationToken cancellationToken) =>
        executor.SingleOrDefaultAsync<EmployeeAttributeValueDto>("dbo.SP_EmployeeAttributeValue_Get",
            new { EmployeeId = employeeId, AttributeId = attributeId }, cancellationToken);

    public Task<IReadOnlyList<EmployeeFormAttributeDto>> ListAsync(long employeeId, bool includeStale,
        CancellationToken cancellationToken) =>
        executor.ListAsync<EmployeeFormAttributeDto>("dbo.SP_EmployeeAttributeValue_List",
            new { EmployeeId = employeeId, IncludeStale = includeStale }, cancellationToken);

    public Task<ValueSaveResultDto> SaveAsync(ValueSaveRequest request, CancellationToken cancellationToken) =>
        executor.SingleAsync<ValueSaveResultDto>("dbo.SP_EmployeeAttributeValue_Save", request, cancellationToken);
}

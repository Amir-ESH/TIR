using TIR.Domain.Employees;

namespace TIR.Application.Contracts;

public interface IEmployeeRepository
{
    Task<Employee> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken);
    Task<Employee> UpdateAsync(EmployeeUpdateRequest request, CancellationToken cancellationToken);
    Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken);
    Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken);
    Task<PageResult<EmployeeListItem>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken);
    Task<PageResult<EmployeeListItem>> SearchAdvancedAsync(AdvancedEmployeeSearchRequest request, CancellationToken cancellationToken);
}

public interface IAttributeRepository
{
    Task<AttributeDefinitionDto> CreateAsync(AttributeCreateRequest request, CancellationToken cancellationToken);
    Task<AttributeDefinitionDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<PageResult<AttributeDefinitionDto>> ListAsync(AttributeListRequest request, CancellationToken cancellationToken);
    Task<AttributeDefinitionDto> UpdateAsync(AttributeUpdateRequest request, CancellationToken cancellationToken);
    Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken);
    Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken);
    Task<AttributeOptionDto> CreateOptionAsync(AttributeOptionCreateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttributeOptionDto>> ListOptionsAsync(long attributeId, bool includeDisabled, CancellationToken cancellationToken);
}

public interface IAttributeValueRepository
{
    Task<EmployeeAttributeValueDto?> GetAsync(long employeeId, long attributeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<EmployeeFormAttributeDto>> ListAsync(long employeeId, bool includeStale, CancellationToken cancellationToken);
    Task<ValueSaveResultDto> SaveAsync(ValueSaveRequest request, CancellationToken cancellationToken);
}

public interface IFormulaRepository
{
    Task<AttributeFormulaDto> RegisterAsync(FormulaRegisterRequest request, CancellationToken cancellationToken);
    Task<FormulaDependencyDto> AddDependencyAsync(FormulaDependencyAddRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<FormulaOrderDto>> GetCalculationOrderAsync(CancellationToken cancellationToken);
    Task<FormulaCalculationDto> CalculateAsync(FormulaCalculateRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<FormulaSummaryDto>> CalculateAllAsync(FormulaCalculateAllRequest request, CancellationToken cancellationToken);
    Task<int> CalculateSetAsync(FormulaCalculateSetRequest request, CancellationToken cancellationToken);
    Task<int> MarkExpiredAsync(int maxRows, CancellationToken cancellationToken);
    Task InvalidateAllAsync(long changedAttributeId, CancellationToken cancellationToken);
    Task InvalidateDependentsAsync(long employeeId, long changedAttributeId, CancellationToken cancellationToken);
}

public interface IBulkRepository
{
    Task<Guid> StageAsync(IAsyncEnumerable<BulkEmployeeInput> employees,
        BulkRequestOptions options, CancellationToken cancellationToken);
    Task<BulkReportDto> ProcessAsync(Guid batchId, int employeesPerTransaction, CancellationToken cancellationToken);
    Task<BulkReportDto?> GetReportAsync(Guid batchId, CancellationToken cancellationToken);
    Task DeleteAsync(Guid batchId, CancellationToken cancellationToken);
    Task<int> CleanupAsync(DateTime olderThanUtc, int maxBatches, CancellationToken cancellationToken);
}

using TIR.Domain.Employees;

namespace TIR.Application.Contracts;

// خواندن گروهی مقادیر برای خروجی اکسل بدون فراخوانی تکی هر کارمند.
public interface IEmployeeExcelValueRepository
{
    Task<IReadOnlyList<EmployeeFormAttributeDto>> ReadAsync(IReadOnlyList<long> employeeIds,
        CancellationToken cancellationToken);
}

/// <summary>
/// 
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Employee> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Employee> UpdateAsync(EmployeeUpdateRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PageResult<EmployeeListItem>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PageResult<EmployeeListItem>> SearchAdvancedAsync(AdvancedEmployeeSearchRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// 
/// </summary>
public interface IAttributeRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AttributeDefinitionDto> CreateAsync(AttributeCreateRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AttributeDefinitionDto?> GetAsync(long id, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<PageResult<AttributeDefinitionDto>> ListAsync(AttributeListRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AttributeDefinitionDto> UpdateAsync(AttributeUpdateRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AttributeOptionDto> CreateOptionAsync(AttributeOptionCreateRequest request, CancellationToken cancellationToken);

    Task<AttributeOptionDto> UpdateOptionAsync(AttributeOptionUpdateRequest request, CancellationToken cancellationToken);

    Task<AttributeOptionDto> SetOptionEnabledAsync(AttributeOptionStateRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="attributeId"></param>
    /// <param name="includeDisabled"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<AttributeOptionDto>> ListOptionsAsync(long attributeId, bool includeDisabled, CancellationToken cancellationToken);
}

/// <summary>
/// 
/// </summary>
public interface IAttributeValueRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="employeeId"></param>
    /// <param name="attributeId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<EmployeeAttributeValueDto?> GetAsync(long employeeId, long attributeId, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="employeeId"></param>
    /// <param name="includeStale"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<EmployeeFormAttributeDto>> ListAsync(long employeeId, bool includeStale, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<ValueSaveResultDto> SaveAsync(ValueSaveRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// 
/// </summary>
public interface IFormulaRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<AttributeFormulaDto> RegisterAsync(FormulaRegisterRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<FormulaDependencyDto> AddDependencyAsync(FormulaDependencyAddRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<FormulaOrderDto>> GetCalculationOrderAsync(CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<FormulaCalculationDto> CalculateAsync(FormulaCalculateRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyList<FormulaSummaryDto>> CalculateAllAsync(FormulaCalculateAllRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> CalculateSetAsync(FormulaCalculateSetRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="maxRows"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> MarkExpiredAsync(int maxRows, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="changedAttributeId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task InvalidateAllAsync(long changedAttributeId, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="employeeId"></param>
    /// <param name="changedAttributeId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task InvalidateDependentsAsync(long employeeId, long changedAttributeId, CancellationToken cancellationToken);
}

/// <summary>
/// 
/// </summary>
public interface IBulkRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="employees"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Guid> StageAsync(IAsyncEnumerable<BulkEmployeeInput> employees, BulkRequestOptions options, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchId"></param>
    /// <param name="employeesPerTransaction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<BulkReportDto> ProcessAsync(Guid batchId, int employeesPerTransaction, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<BulkReportDto?> GetReportAsync(Guid batchId, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="batchId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task DeleteAsync(Guid batchId, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="olderThanUtc"></param>
    /// <param name="maxBatches"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> CleanupAsync(DateTime olderThanUtc, int maxBatches, CancellationToken cancellationToken);
}

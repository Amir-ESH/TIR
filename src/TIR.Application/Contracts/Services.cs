using FluentValidation;
using TIR.Domain.Employees;

namespace TIR.Application.Contracts;

/// <summary>
/// سرویس ها فقط سناریوی برنامه و اعتبارسنجی اولیه را هماهنگ می کنند.
/// </summary>
public interface IEmployeeService
{
    Task<Employee> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken = default);
    Task<Employee> UpdateAsync(EmployeeUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken = default);
    Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken = default);
    Task<PageResult<EmployeeListItem>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken = default);
    Task<PageResult<EmployeeListItem>> SearchAdvancedAsync(AdvancedEmployeeSearchRequest request, CancellationToken cancellationToken = default);
}

public sealed class EmployeeService(
    IEmployeeRepository repository,
    IValidator<EmployeeCreateRequest> createValidator,
    IValidator<EmployeeUpdateRequest> updateValidator,
    IValidator<StateChangeRequest> stateValidator,
    IValidator<EmployeeListRequest> listValidator,
    IValidator<AdvancedEmployeeSearchRequest> searchValidator) : IEmployeeService
{
    public async Task<Employee> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.CreateAsync(request, cancellationToken);
    }

    public async Task<Employee> UpdateAsync(EmployeeUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.UpdateAsync(request, cancellationToken);
    }

    public async Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await stateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.SoftDeleteAsync(request, cancellationToken);
    }

    public async Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await stateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.RestoreAsync(request, cancellationToken);
    }

    public async Task<PageResult<EmployeeListItem>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await listValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.ListAsync(request, cancellationToken);
    }

    public async Task<PageResult<EmployeeListItem>> SearchAdvancedAsync(AdvancedEmployeeSearchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await searchValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.SearchAdvancedAsync(request, cancellationToken);
    }
}

public interface IAttributeService
{
    Task<AttributeDefinitionDto> CreateAsync(AttributeCreateRequest request, CancellationToken cancellationToken = default);
    Task<AttributeDefinitionDto?> GetAsync(long id, CancellationToken cancellationToken = default);
    Task<PageResult<AttributeDefinitionDto>> ListAsync(AttributeListRequest request, CancellationToken cancellationToken = default);
    Task<AttributeDefinitionDto> UpdateAsync(AttributeUpdateRequest request, CancellationToken cancellationToken = default);
    Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken = default);
    Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken = default);
    Task<AttributeOptionDto> CreateOptionAsync(AttributeOptionCreateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttributeOptionDto>> ListOptionsAsync(long attributeId, bool includeDisabled, CancellationToken cancellationToken = default);
}

public sealed class AttributeService(
    IAttributeRepository repository,
    IValidator<AttributeCreateRequest> createValidator,
    IValidator<AttributeUpdateRequest> updateValidator,
    IValidator<AttributeListRequest> listValidator,
    IValidator<AttributeOptionCreateRequest> optionValidator,
    IValidator<StateChangeRequest> stateValidator) : IAttributeService
{
    public async Task<AttributeDefinitionDto> CreateAsync(AttributeCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.CreateAsync(request, cancellationToken);
    }

    public Task<AttributeDefinitionDto?> GetAsync(long id, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return repository.GetAsync(id, cancellationToken);
    }

    public async Task<PageResult<AttributeDefinitionDto>> ListAsync(AttributeListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await listValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.ListAsync(request, cancellationToken);
    }

    public async Task<AttributeDefinitionDto> UpdateAsync(AttributeUpdateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.UpdateAsync(request, cancellationToken);
    }

    public async Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await stateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.SoftDeleteAsync(request, cancellationToken);
    }

    public async Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await stateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.RestoreAsync(request, cancellationToken);
    }

    public async Task<AttributeOptionDto> CreateOptionAsync(AttributeOptionCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await optionValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.CreateOptionAsync(request, cancellationToken);
    }

    public Task<IReadOnlyList<AttributeOptionDto>> ListOptionsAsync(long attributeId, bool includeDisabled, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attributeId);
        return repository.ListOptionsAsync(attributeId, includeDisabled, cancellationToken);
    }
}

public interface IAttributeValueService
{
    Task<EmployeeAttributeValueDto?> GetAsync(long employeeId, long attributeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmployeeFormAttributeDto>> ListAsync(long employeeId, bool includeStale = true, CancellationToken cancellationToken = default);
    Task<ValueSaveResultDto> SaveAsync(ValueSaveRequest request, CancellationToken cancellationToken = default);
}

public sealed class AttributeValueService(
    IAttributeValueRepository repository,
    IValidator<ValueSaveRequest> saveValidator) : IAttributeValueService
{
    public Task<EmployeeAttributeValueDto?> GetAsync(long employeeId, long attributeId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(employeeId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attributeId);
        return repository.GetAsync(employeeId, attributeId, cancellationToken);
    }

    public Task<IReadOnlyList<EmployeeFormAttributeDto>> ListAsync(long employeeId, bool includeStale = true, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(employeeId);
        return repository.ListAsync(employeeId, includeStale, cancellationToken);
    }

    public async Task<ValueSaveResultDto> SaveAsync(ValueSaveRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await saveValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.SaveAsync(request, cancellationToken);
    }
}

public interface IFormulaService
{
    Task<AttributeFormulaDto> RegisterAsync(FormulaRegisterRequest request, CancellationToken cancellationToken = default);
    Task<FormulaDependencyDto> AddDependencyAsync(FormulaDependencyAddRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormulaOrderDto>> GetCalculationOrderAsync(CancellationToken cancellationToken = default);
    Task<FormulaCalculationDto> CalculateAsync(FormulaCalculateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FormulaSummaryDto>> CalculateAllAsync(FormulaCalculateAllRequest request, CancellationToken cancellationToken = default);
    Task<int> CalculateSetAsync(FormulaCalculateSetRequest request, CancellationToken cancellationToken = default);
    Task<int> MarkExpiredAsync(int maxRows = 10000, CancellationToken cancellationToken = default);
    Task InvalidateAllAsync(long changedAttributeId, CancellationToken cancellationToken = default);
    Task InvalidateDependentsAsync(long employeeId, long changedAttributeId, CancellationToken cancellationToken = default);
}

public sealed class FormulaService(
    IFormulaRepository repository,
    IValidator<FormulaRegisterRequest> registerValidator,
    IValidator<FormulaDependencyAddRequest> dependencyValidator,
    IValidator<FormulaCalculateRequest> calculateValidator,
    IValidator<FormulaCalculateAllRequest> calculateAllValidator,
    IValidator<FormulaCalculateSetRequest> calculateSetValidator) : IFormulaService
{
    public async Task<AttributeFormulaDto> RegisterAsync(FormulaRegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.RegisterAsync(request, cancellationToken);
    }

    public async Task<FormulaDependencyDto> AddDependencyAsync(FormulaDependencyAddRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await dependencyValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.AddDependencyAsync(request, cancellationToken);
    }

    public Task<IReadOnlyList<FormulaOrderDto>> GetCalculationOrderAsync(CancellationToken cancellationToken = default) =>
        repository.GetCalculationOrderAsync(cancellationToken);

    public async Task<FormulaCalculationDto> CalculateAsync(FormulaCalculateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await calculateValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.CalculateAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<FormulaSummaryDto>> CalculateAllAsync(FormulaCalculateAllRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await calculateAllValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.CalculateAllAsync(request, cancellationToken);
    }

    public async Task<int> CalculateSetAsync(FormulaCalculateSetRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await calculateSetValidator.ValidateAndThrowAsync(request, cancellationToken);
        return await repository.CalculateSetAsync(request, cancellationToken);
    }

    public Task<int> MarkExpiredAsync(int maxRows = 10000, CancellationToken cancellationToken = default)
    {
        if (maxRows is < 1 or > 100000) throw new ArgumentOutOfRangeException(nameof(maxRows));
        return repository.MarkExpiredAsync(maxRows, cancellationToken);
    }

    public Task InvalidateAllAsync(long changedAttributeId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(changedAttributeId);
        return repository.InvalidateAllAsync(changedAttributeId, cancellationToken);
    }

    public Task InvalidateDependentsAsync(long employeeId, long changedAttributeId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(employeeId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(changedAttributeId);
        return repository.InvalidateDependentsAsync(employeeId, changedAttributeId, cancellationToken);
    }
}

public interface IBulkService
{
    Task<BulkReportDto> UploadAndProcessAsync(
        IAsyncEnumerable<BulkEmployeeInput> employees, BulkRequestOptions options,
        CancellationToken cancellationToken = default);
    Task<Guid> StageAsync(IAsyncEnumerable<BulkEmployeeInput> employees, BulkRequestOptions options,
        CancellationToken cancellationToken = default);
    Task<BulkReportDto> ProcessAsync(Guid batchId, int employeesPerTransaction = 2000,
        CancellationToken cancellationToken = default);
    Task<BulkReportDto?> GetReportAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid batchId, CancellationToken cancellationToken = default);
    Task<int> CleanupAsync(DateTime olderThanUtc, int maxBatches = 1000,
        CancellationToken cancellationToken = default);
}

public sealed class BulkService(IBulkRepository repository,
    IValidator<BulkRequestOptions> optionsValidator) : IBulkService
{
    public async Task<BulkReportDto> UploadAndProcessAsync(
        IAsyncEnumerable<BulkEmployeeInput> employees, BulkRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        var id = await StageAsync(employees, options, cancellationToken);
        return await ProcessAsync(id, options.EmployeesPerTransaction, cancellationToken);
    }

    public async Task<Guid> StageAsync(IAsyncEnumerable<BulkEmployeeInput> employees, BulkRequestOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(employees);
        ArgumentNullException.ThrowIfNull(options);
        await optionsValidator.ValidateAndThrowAsync(options, cancellationToken);
        return await repository.StageAsync(employees, options, cancellationToken);
    }

    public Task<BulkReportDto> ProcessAsync(Guid batchId, int employeesPerTransaction = 2000,
        CancellationToken cancellationToken = default)
    {
        if (batchId == Guid.Empty || employeesPerTransaction is < 100 or > 10000)
            throw new ArgumentException("شناسه درخواست یا اندازه بخش معتبر نیست.");
        return repository.ProcessAsync(batchId, employeesPerTransaction, cancellationToken);
    }

    public Task<BulkReportDto?> GetReportAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        if (batchId == Guid.Empty) throw new ArgumentException("شناسه درخواست نامعتبر است.");
        return repository.GetReportAsync(batchId, cancellationToken);
    }

    public Task DeleteAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        if (batchId == Guid.Empty) throw new ArgumentException("شناسه درخواست نامعتبر است.");
        return repository.DeleteAsync(batchId, cancellationToken);
    }

    public Task<int> CleanupAsync(DateTime olderThanUtc, int maxBatches = 1000,
        CancellationToken cancellationToken = default)
    {
        if (olderThanUtc.Kind != DateTimeKind.Utc || maxBatches is < 1 or > 10000)
            throw new ArgumentException("زمان UTC یا اندازه پاکسازی معتبر نیست.");
        return repository.CleanupAsync(olderThanUtc, maxBatches, cancellationToken);
    }
}

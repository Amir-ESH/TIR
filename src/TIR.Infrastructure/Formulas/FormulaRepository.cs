using TIR.Application.Contracts;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Formulas;

internal sealed class FormulaRepository(StoredProcedureExecutor executor) : IFormulaRepository
{
    public Task<AttributeFormulaDto> RegisterAsync(FormulaRegisterRequest request, CancellationToken cancellationToken)
        => executor.SingleAsync<AttributeFormulaDto>("dbo.SP_AttributeFormula_Register", request, cancellationToken);

    public Task<FormulaDependencyDto> AddDependencyAsync(FormulaDependencyAddRequest request, CancellationToken cancellationToken)
        => executor.SingleAsync<FormulaDependencyDto>("dbo.SP_FormulaDependency_Add", request, cancellationToken);

    public Task<IReadOnlyList<FormulaOrderDto>> GetCalculationOrderAsync(CancellationToken cancellationToken)
        => executor.ListAsync<FormulaOrderDto>("dbo.SP_Formula_GetCalculationOrder", null, cancellationToken);

    public Task<FormulaCalculationDto> CalculateAsync(FormulaCalculateRequest request, CancellationToken cancellationToken)
        => executor.SingleAsync<FormulaCalculationDto>("dbo.SP_Formula_Calculate", request, cancellationToken);

    public async Task<IReadOnlyList<FormulaSummaryDto>> CalculateAllAsync(
        FormulaCalculateAllRequest request, CancellationToken cancellationToken)
    {
        await executor.ExecuteAsync("dbo.SP_Formula_CalculateAll", request, cancellationToken, timeoutSeconds: 600);
        
        var result = await executor.ListAsync<FormulaSummaryDto>("dbo.SP_EmployeeAttributeValue_List",
            new { request.EmployeeId, IncludeStale = true }, cancellationToken);

        return result.Where(x => x.CalculationStatus.HasValue).ToList();
    }

    public Task<int> CalculateSetAsync(FormulaCalculateSetRequest request, CancellationToken cancellationToken)
        => executor.SingleAsync<int>("dbo.SP_Formula_CalculateSet", request, cancellationToken, timeoutSeconds: 600);

    public Task<int> MarkExpiredAsync(int maxRows, CancellationToken cancellationToken)
        => executor.SingleAsync<int>("dbo.SP_Formula_MarkExpired", new { MaxRows = maxRows }, cancellationToken);

    public Task InvalidateAllAsync(long changedAttributeId, CancellationToken cancellationToken)
        => executor.ExecuteAsync("dbo.SP_Formula_InvalidateAll",
            new { ChangedAttributeId = changedAttributeId }, cancellationToken);

    public Task InvalidateDependentsAsync(long employeeId, long changedAttributeId, CancellationToken cancellationToken)
        => executor.ExecuteAsync("dbo.SP_Formula_InvalidateDependents",
            new { EmployeeId = employeeId, ChangedAttributeId = changedAttributeId }, cancellationToken);
}

using System.Data;
using System.Text.Json;
using Dapper;
using TIR.Application.Contracts;
using TIR.Domain.Employees;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Employees;

/// <summary>
/// 
/// </summary>
/// <param name="executor"></param>
internal sealed class EmployeeRepository(StoredProcedureExecutor executor) : IEmployeeRepository
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Employee> CreateAsync(EmployeeCreateRequest request, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("EmployeeCode", request.EmployeeCode);
        parameters.Add("FirstName", request.FirstName);
        parameters.Add("LastName", request.LastName);
        // مسیر واحد: TVP خالی به معنی نداشتن مشخصه اولیه است.
        var initialValues = request.InitialValues ?? Array.Empty<TypedValueInput>();
        parameters.Add("Values", CreateInitialValuesTable(initialValues).AsTableValuedParameter("dbo.EmployeeNewValueType"));

        var row = await executor.SingleAsync<EmployeeDbRow>("dbo.SP_Employee_Create", parameters, cancellationToken);
        return EmployeeDbMapper.ToDomain(row);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Employee> UpdateAsync(EmployeeUpdateRequest request, CancellationToken cancellationToken)
    {
        var row = await executor.SingleAsync<EmployeeDbRow>("dbo.SP_Employee_Update", request, cancellationToken);
        return EmployeeDbMapper.ToDomain(row);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ChangeStateDto> SoftDeleteAsync(StateChangeRequest request, CancellationToken cancellationToken)
        => executor.SingleAsync<ChangeStateDto>("dbo.SP_Employee_SoftDelete", request, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<ChangeStateDto> RestoreAsync(StateChangeRequest request, CancellationToken cancellationToken)
        => executor.SingleAsync<ChangeStateDto>("dbo.SP_Employee_Restore", request, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<PageResult<EmployeeListItem>> ListAsync(EmployeeListRequest request, CancellationToken cancellationToken)
        => executor.PageAsync<EmployeeListItem>("dbo.SP_Employee_List", request, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<PageResult<EmployeeListItem>> SearchAdvancedAsync(AdvancedEmployeeSearchRequest request, CancellationToken cancellationToken)
    {
        // JSON مقدارها را به صورت داده ارسال می کند. SQL نام ستون و عملگر را کنترل می کند.
        var filtersJson = JsonSerializer.Serialize(request.Filters.Select(x => new
        {
            group = x.Group,
            attributeId = x.AttributeId,
            @operator = x.Operator,
            value = x.Value,
            to = x.To
        }));

        return executor.PageAsync<EmployeeListItem>("dbo.SP_Employee_SearchAdvanced", new
        {
            FiltersJson = filtersJson,
            request.EmployeeSearch,
            request.SortBy,
            request.SortAttributeId,
            request.SortDescending,
            request.PageNumber,
            request.PageSize
        }, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    private static DataTable CreateInitialValuesTable(IReadOnlyList<TypedValueInput> values)
    {
        var table = new DataTable();
        table.Columns.Add("AttributeId", typeof(long));
        table.Columns.Add("StringValue", typeof(string));
        table.Columns.Add("IntegerValue", typeof(long));
        table.Columns.Add("DecimalValue", typeof(decimal));
        table.Columns.Add("DateValue", typeof(DateTime));
        table.Columns.Add("TimeValue", typeof(TimeSpan));
        table.Columns.Add("DateTimeUtc", typeof(DateTime));
        table.Columns.Add("BooleanValue", typeof(bool));
        table.Columns.Add("OptionId", typeof(long));

        foreach (var value in values)
            table.Rows.Add(value.AttributeId, Db(value.StringValue), Db(value.IntegerValue),
                Db(value.DecimalValue), Db(value.DateValue?.Date), Db(value.TimeValue),
                Db(value.DateTimeUtc), Db(value.BooleanValue), Db(value.OptionId));

        return table;

        static object Db(object? value)
            => value ?? DBNull.Value;
    }
}

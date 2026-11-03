using TIR.Application.Contracts;

namespace TIR.Web.Presentation;

// خروجی اکسل فهرست و جستجوی پیشرفته از یک مسیر مشترک استفاده می کند.
public sealed class EmployeeExcelExportService(
    IEmployeeService employees,
    IAttributeService attributes,
    IEmployeeExcelValueRepository values)
{
    public async Task<byte[]> TemplateAsync(bool update, CancellationToken cancellationToken)
    {
        var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
        var options = await LoadOptionsAsync(definitions, cancellationToken);
        return BulkExcelWorkbook.Template(update, definitions, options);
    }

    public Task<byte[]> ExportListAsync(EmployeeListRequest request, CancellationToken cancellationToken)
        => ExportAsync((page, size) => employees.ListAsync(
            request with { PageNumber = page, PageSize = size, IncludeDeleted = false }, cancellationToken), cancellationToken);

    public Task<byte[]> ExportSearchAsync(AdvancedEmployeeSearchRequest request, CancellationToken cancellationToken)
        => ExportAsync((page, size) => employees.SearchAdvancedAsync(
            request with { PageNumber = page, PageSize = size }, cancellationToken), cancellationToken);

    private async Task<byte[]> ExportAsync(
        Func<int, int, Task<PageResult<EmployeeListItem>>> fetch,
        CancellationToken cancellationToken)
    {
        var first = await fetch(1, 200);
        if (first.TotalCount > BulkExcelWorkbook.MaximumRows)
            throw new InvalidOperationException($"تعداد نتایج بیشتر از {BulkExcelWorkbook.MaximumRows:N0} است؛ فیلتر را محدود کنید.");

        var records = new List<EmployeeListItem>(first.Items);
        for (var page = 2; records.Count < first.TotalCount; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await fetch(page, 200);
            if (current.Items.Count == 0) break;
            records.AddRange(current.Items);
        }
        var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
        var options = await LoadOptionsAsync(definitions, cancellationToken);
        var snapshot = new Dictionary<long, IReadOnlyList<EmployeeFormAttributeDto>>(records.Count);
        for (var start = 0; start < records.Count; start += 200)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ids = records.Skip(start).Take(200).Select(x => x.Id).ToArray();
            var valuesForEmployees = await values.ReadAsync(ids, cancellationToken);
            var grouped = valuesForEmployees.GroupBy(x => x.EmployeeId)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<EmployeeFormAttributeDto>)x.ToList());
            foreach (var employeeId in ids)
                snapshot[employeeId] = grouped.TryGetValue(employeeId, out var selected) ? selected : [];
        }
        return BulkExcelWorkbook.Export(true, definitions, records, snapshot, options);
    }

    private async Task<IReadOnlyList<AttributeOptionDto>> LoadOptionsAsync(
        IReadOnlyList<AttributeDefinitionDto> definitions, CancellationToken cancellationToken)
    {
        var all = new List<AttributeOptionDto>();
        foreach (var definition in definitions.Where(x => x.DataKind == 8))
            all.AddRange(await attributes.ListOptionsAsync(definition.Id, false, cancellationToken));
        return all;
    }
}

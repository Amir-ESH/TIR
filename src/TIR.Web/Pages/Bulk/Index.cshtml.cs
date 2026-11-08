using System.Text.Json;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Jobs;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Bulk;

public sealed class IndexModel(
    IBulkWizardRepository wizard,
    IEmployeeService employees,
    IAttributeService attributes,
    IFormulaService formulas,
    EmployeeExcelExportService excel,
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<IndexModel> logger) : PageModel
{
    private const int ChunkSize = 10000;

    public bool HangfireEnabled => configuration.GetValue<bool>("Hangfire:Enabled");

    public string AttributeJson { get; private set; } = "[]";

    public List<AttributeDefinitionDto> Attributes { get; private set; } = [];

    public List<(long Id, string Title)> FormulaList { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => await LoadMetadataAsync(cancellationToken);

    public async Task<IActionResult> OnGetTemplateAsync(string operation, CancellationToken cancellationToken)
    {
        if (operation is not ("create" or "update"))
            return BadRequest("نوع عملیات معتبر نیست.");

        var file = await excel.TemplateAsync(operation == "update", cancellationToken);

        return File(file, BulkExcelWorkbook.ContentType,
            operation == "update" ? "Employees_Update_Template.xlsx" : "Employees_Create_Template.xlsx");
    }

    public async Task<IActionResult> OnPostPrepareAsync(byte operationKind,
        IFormFile? excelFile, CancellationToken cancellationToken)
    {
        if (operationKind is not (1 or 2))
            return BadRequest(new { error = "نوع عملیات معتبر نیست." });

        if (excelFile is null || excelFile.Length == 0 ||
            !Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "فایل اکسل با فرمت xlsx انتخاب کنید." });

        try
        {
            var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
            await using var stream = excelFile.OpenReadStream();
            var entries = BulkExcelReader.Read(stream, operationKind == 2, definitions);

            var rows = entries.Select((entry, index) => new BulkWizardEntry(index + 1,
                entry.ExcelRow, entry.EmployeeId, entry.ExpectedEmployeeRowVersion,
                entry.EmployeeCode, entry.FirstName, entry.LastName, entry.Values)).ToList();

            var id = await wizard.StageAsync(operationKind, null, rows, cancellationToken);

            return new JsonResult(new { workId = id, total = rows.Count,
                message = "فایل بررسی و برای پردازش آماده شد. برای ثبت تغییرات، اجرای عملیات را تایید کنید." });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در آماده سازی فایل اکسل عملیات گروهی");
            return BadRequest(new { error = UiErrors.Message(error) });
        }
    }

    public async Task<IActionResult> OnPostPrepareFormulaAsync(long formulaId, string selectionMode,
        string? selectedIdsJson, string? filtersJson, string? employeeSearch,
        CancellationToken cancellationToken)
    {
        if (formulaId <= 0)
            return BadRequest(new { error = "فرمول انتخاب نشده است." });

        try
        {
            Guid id;
            int total;
            if (selectionMode == "all")
            {
                (id, total) = await wizard.StageFormulaAllAsync(formulaId, cancellationToken);
            }
            else
            {
                IReadOnlyList<long> employeeIds;

                if (selectionMode == "selected")
                {
                    var ids = JsonSerializer.Deserialize<List<long>>(selectedIdsJson ?? "[]") ?? [];
                    employeeIds = ids.Where(x => x > 0).Distinct().ToArray();
                }
                else if (selectionMode == "filtered")
                {
                    var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
                    var filters = AdvancedSearchInput.Parse(filtersJson ?? "[]", definitions);
                    var collected = new List<long>();

                    for (var page = 1; ; page++)
                    {
                        var result = await employees.SearchAdvancedAsync(new AdvancedEmployeeSearchRequest(
                            filters, employeeSearch, "Id", null, false, page, 200), cancellationToken);

                        collected.AddRange(result.Items.Select(x => x.Id));

                        if (result.Items.Count < 200)
                            break;
                    }
                    employeeIds = collected.Distinct().ToArray();
                }
                else
                    return BadRequest(new { error = "روش انتخاب کارمندان معتبر نیست." });

                if (employeeIds.Count == 0)
                    return BadRequest(new { error = "حداقل یک کارمند را انتخاب کنید." });

                var rows = employeeIds.Select((employeeId, index) => new BulkWizardEntry(
                    index + 1, null, employeeId, null, null, null, null, [])).ToArray();

                id = await wizard.StageAsync(3, formulaId, rows, cancellationToken);

                total = rows.Length;
            }
            return new JsonResult(new { workId = id, total,
                message = "کارمندان انتخابی آماده اجرای فرمول هستند." });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در آماده سازی گروه فرمول");
            return BadRequest(new { error = UiErrors.Message(error) });
        }
    }

    public async Task<IActionResult> OnPostSearchEmployeesAsync(string filtersJson,
        string? employeeSearch, string? sortBy, long? sortAttributeId,
        bool sortDescending, int pageNumber, CancellationToken cancellationToken)
    {
        try
        {
            var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
            var filters = AdvancedSearchInput.Parse(filtersJson ?? "[]", definitions);
            var safeSortBy = sortBy ?? "Id";

            if (safeSortBy is not ("Id" or "EmployeeCode" or "FirstName" or "LastName"))
                return BadRequest(new { error = "ستون مرتب سازی نامعتبر است." });

            var result = await employees.SearchAdvancedAsync(new AdvancedEmployeeSearchRequest(
                filters, employeeSearch, safeSortBy, sortAttributeId,
                sortDescending, Math.Max(1, pageNumber), 50), cancellationToken);

            return new JsonResult(new { totalCount = result.TotalCount, pageNumber = Math.Max(1, pageNumber),
                items = result.Items.Select(x => new { x.Id, x.EmployeeCode, x.FirstName, x.LastName }) });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در جستجوی انتخابی کارمندان");
            return BadRequest(new { error = UiErrors.Message(error) });
        }
    }

    public async Task<IActionResult> OnPostExecuteAsync(Guid workId, CancellationToken cancellationToken)
    {
        if (!HangfireEnabled || services.GetService<IBackgroundJobClient>() is not { } jobs)
            return StatusCode(503, new { error = "برای اجرای عملیات گروهی باید Hangfire فعال باشد." });

        try
        {
            var (kind, total) = await wizard.QueueAsync(workId, cancellationToken);

            string? previous = null;

            for (var part = 0; part < (total + ChunkSize - 1) / ChunkSize; part++)
            {
                var chunk = part;
                previous = previous is null
                    ? jobs.Enqueue<BulkWizardJob>(x => x.ProcessAsync(workId, chunk, kind, CancellationToken.None))
                    : jobs.ContinueJobWith<BulkWizardJob>(previous, x => x.ProcessAsync(workId, chunk, kind, CancellationToken.None));
            }

            return new JsonResult(new { workId, total, message = "درخواست به صف پردازش ارسال شد." });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در صف عملیات گروهی {WorkId}", workId);
            return BadRequest(new { error = UiErrors.Message(error) });
        }
    }

    public async Task<IActionResult> OnGetStatusAsync(Guid workId, int errorPage = 1,
        CancellationToken cancellationToken = default)
    {
        var report = await wizard.ReportAsync(workId, Math.Max(1, errorPage), cancellationToken);

        return report is null ? NotFound() : new JsonResult(report);
    }

    private async Task LoadMetadataAsync(CancellationToken cancellationToken)
    {
        var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);

        Attributes = definitions;
        AttributeJson = JsonSerializer.Serialize(definitions.Select(x => new
        {
            id = x.Id, title = x.Title, kind = x.DataKind, required = x.IsRequired
        }));

        var order = await formulas.GetCalculationOrderAsync(cancellationToken);

        var byId = definitions.ToDictionary(x => x.Id);

        FormulaList = order.Where(x => byId.ContainsKey(x.AttributeId))
            .Select(x => (x.FormulaId, byId[x.AttributeId].Title)).ToList();
    }
}

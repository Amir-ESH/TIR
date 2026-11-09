using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class IndexModel(IEmployeeService employees, EmployeeExcelExportService excel, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public bool IncludeDeleted { get; set; }
    [BindProperty(SupportsGet = true)] public string SortBy { get; set; } = "Id";
    [BindProperty(SupportsGet = true)] public bool SortDescending { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public PageResult<EmployeeListItem> Result { get; private set; } = new(0, []);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PageNumber = Math.Max(1, PageNumber);
        try
        {
            Result = await employees.ListAsync(new EmployeeListRequest(
                Search, IncludeDeleted, SortBy, SortDescending, PageNumber), cancellationToken);
        }
        catch (Exception error) { logger.LogError(error, "خطا در فهرست کارمندان"); TempData["Error"] = UiErrors.Message(error); }
    }

    public async Task<IActionResult> OnGetExcelAsync(CancellationToken cancellationToken)
    {
        try
        {
            var file = await excel.ExportListAsync(new EmployeeListRequest(
                Search, false, SortBy, SortDescending), cancellationToken);
            return File(file, BulkExcelWorkbook.ContentType, "Employees_Export.xlsx");
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در خروجی اکسل کارمندان");
            TempData["Error"] = UiErrors.Message(error);
            return RedirectToPage("/Employees/Index");
        }
    }

    public Task<IActionResult> OnPostSoftDeleteAsync(long id, string rowVersion, CancellationToken cancellationToken) =>
        ChangeStateAsync(id, rowVersion, false, cancellationToken);

    public Task<IActionResult> OnPostRestoreAsync(long id, string rowVersion, CancellationToken cancellationToken) =>
        ChangeStateAsync(id, rowVersion, true, cancellationToken);

    private async Task<IActionResult> ChangeStateAsync(long id, string rowVersion, bool restore, CancellationToken cancellationToken)
    {
        var ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        try
        {
            var version = UiFieldFactory.DecodeRowVersion(rowVersion)
                ?? throw new FormatException("نسخه کارمند در درخواست وجود ندارد.");
            if (restore) await employees.RestoreAsync(new StateChangeRequest(id, version), cancellationToken);
            else await employees.SoftDeleteAsync(new StateChangeRequest(id, version), cancellationToken);
            var message = restore ? "کارمند بازیابی شد." : "کارمند حذف منطقی شد.";
            if (ajax) return new JsonResult(new { success = true, message });
            TempData["Success"] = message;
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در تغییر وضعیت کارمند {Id}", id);
            var message = UiErrors.Message(error);
            if (ajax) return new JsonResult(new { success = false, message }) { StatusCode = 400 };
            TempData["Error"] = message;
        }
        return RedirectToPage("/Employees/Index", new { IncludeDeleted = true });
    }
}

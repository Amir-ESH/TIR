using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class IndexModel(IEmployeeService employees, ILogger<IndexModel> logger) : PageModel
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

    public Task<IActionResult> OnPostSoftDeleteAsync(long id, string rowVersion, CancellationToken cancellationToken) =>
        ChangeStateAsync(id, rowVersion, false, cancellationToken);

    public Task<IActionResult> OnPostRestoreAsync(long id, string rowVersion, CancellationToken cancellationToken) =>
        ChangeStateAsync(id, rowVersion, true, cancellationToken);

    private async Task<IActionResult> ChangeStateAsync(long id, string rowVersion, bool restore, CancellationToken cancellationToken)
    {
        try
        {
            var version = UiFieldFactory.DecodeRowVersion(rowVersion)
                ?? throw new FormatException("نسخه کارمند در درخواست وجود ندارد.");
            if (restore) await employees.RestoreAsync(new StateChangeRequest(id, version), cancellationToken);
            else await employees.SoftDeleteAsync(new StateChangeRequest(id, version), cancellationToken);
            TempData["Success"] = restore ? "کارمند بازیابی شد." : "کارمند حذف منطقی شد.";
        }
        catch (Exception error) { logger.LogError(error, "خطا در تغییر وضعیت کارمند {Id}", id); TempData["Error"] = UiErrors.Message(error); }
        return RedirectToPage("/Employees/Index", new { IncludeDeleted = true });
    }
}

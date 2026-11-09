using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Attributes;

public sealed class IndexModel(IAttributeService attributes, ILogger<IndexModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public bool IncludeDeleted { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public PageResult<AttributeDefinitionDto> Result { get; private set; } = new(0, []);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        PageNumber = Math.Max(1, PageNumber);
        try { Result = await attributes.ListAsync(new AttributeListRequest(Search, IncludeDeleted, PageNumber), cancellationToken); }
        catch (Exception error) { logger.LogError(error, "خطا در فهرست مشخصه ها"); TempData["Error"] = UiErrors.Message(error); }
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
                ?? throw new FormatException("نسخه مشخصه ارسال نشده است.");
            if (restore) await attributes.RestoreAsync(new StateChangeRequest(id, version), cancellationToken);
            else await attributes.SoftDeleteAsync(new StateChangeRequest(id, version), cancellationToken);
            var message = restore ? "مشخصه بازیابی شد." : "مشخصه حذف منطقی شد.";
            if (ajax) return new JsonResult(new { success = true, message });
            TempData["Success"] = message;
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در تغییر وضعیت مشخصه {Id}", id);
            var message = UiErrors.Message(error);
            if (ajax) return new JsonResult(new { success = false, message }) { StatusCode = 400 };
            TempData["Error"] = message;
        }
        return RedirectToPage("/Attributes/Index", new { IncludeDeleted = true });
    }

    public string DataKindTitle(byte kind) => kind switch
    {
        1 => "متن", 2 => "صحیح", 3 => "اعشار", 4 => "تاریخ", 5 => "ساعت",
        6 => "تاریخ و ساعت", 7 => "بولین", 8 => "انتخاب تکی", _ => "نامشخص"
    };
}

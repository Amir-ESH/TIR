using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Bulk;

public sealed class ReportModel(IBulkService bulk, ILogger<ReportModel> logger) : PageModel
{
    public BulkReportDto? Report { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Report = await bulk.GetReportAsync(id, cancellationToken);

        if (Report is null)
            return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        try
        {
            await bulk.DeleteAsync(id, cancellationToken);

            if (ajax)
                return new JsonResult(new { success = true, message = "اطلاعات موقت درخواست حذف شدند." });

            TempData["Success"] = "اطلاعات موقت درخواست به صورت فیزیکی حذف شدند.";
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در پاکسازی درخواست {BatchId}", id);
            var message = UiErrors.Message(error);

            if (ajax)
                return new JsonResult(new { success = false, message }) { StatusCode = 400 };

            TempData["Error"] = message;

            return RedirectToPage("/Bulk/Report", new { id });
        }

        return RedirectToPage("/Bulk/Index");
    }
}

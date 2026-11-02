using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Bulk;

public sealed class IndexModel(IBulkService bulk, IAttributeService attributes,
    ILogger<IndexModel> logger) : PageModel
{
    [BindProperty] public IFormFile? CsvFile { get; set; }
    [BindProperty] public int TransferBatchSize { get; set; } = 3000;
    [BindProperty] public int EmployeesPerTransaction { get; set; } = 2000;
    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (CsvFile is null || CsvFile.Length == 0)
            ModelState.AddModelError(string.Empty, "فایل CSV انتخاب نشده است.");
        if (CsvFile is { Length: > 100L * 1024 * 1024 })
            ModelState.AddModelError(string.Empty, "حجم فایل بیشتر از 100 مگابایت است.");
        if (!ModelState.IsValid) return Page();

        try
        {
            var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
            var kinds = definitions.ToDictionary(x => x.Id, x => x.DataKind);
            await using var stream = CsvFile!.OpenReadStream();
            var rows = BulkCsvReader.ReadAsync(stream, kinds, cancellationToken);
            var report = await bulk.UploadAndProcessAsync(rows,
                new BulkRequestOptions(TransferBatchSize, EmployeesPerTransaction), cancellationToken);
            TempData["Success"] = "پردازش گروهی انجام شد. گزارش را بررسی کنید.";
            return RedirectToPage("/Bulk/Report", new { id = report.Header.BatchId });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در پردازش فایل گروهی");
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
            return Page();
        }
    }
}

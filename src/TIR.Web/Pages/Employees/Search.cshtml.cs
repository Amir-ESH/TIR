using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Common;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class SearchModel(IEmployeeService employees, IAttributeService attributes, EmployeeExcelExportService excel,
    ILogger<SearchModel> logger) : PageModel
{
    [BindProperty] public string FiltersJson { get; set; } = "[]";
    [BindProperty] public string? EmployeeSearch { get; set; }
    [BindProperty] public string SortBy { get; set; } = "Id";
    [BindProperty] public long? SortAttributeId { get; set; }
    [BindProperty] public bool SortDescending { get; set; }
    [BindProperty] public int PageNumber { get; set; } = 1;

    public bool HasSearched { get; private set; }
    public PageResult<EmployeeListItem> Result { get; private set; } = new(0, []);
    public List<AttributeDefinitionDto> Attributes { get; private set; } = [];
    public string AttributeJson { get; private set; } = "[]";
    private AdvancedEmployeeSearchRequest? ExportRequest { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAttributesAsync(cancellationToken);

    // گزینه های انتخاب تکی فقط هنگام انتخاب مشخصه بارگذاری می شوند.
    public async Task<IActionResult> OnGetOptionsAsync(long attributeId, CancellationToken cancellationToken)
    {
        var definition = await attributes.GetAsync(attributeId, cancellationToken);
        if (definition is null || definition.IsDeleted || definition.DataKind != 8)
            return NotFound();
        var options = await attributes.ListOptionsAsync(attributeId, false, cancellationToken);
        return new JsonResult(options.Select(x => new { id = x.Id, title = x.Title }));
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        HasSearched = true;
        await LoadAttributesAsync(cancellationToken);
        try
        {
            var normalized = AdvancedSearchInput.Parse(FiltersJson, Attributes);

            ExportRequest = new AdvancedEmployeeSearchRequest(
                normalized, EmployeeSearch, SortBy, SortAttributeId, SortDescending,
                Math.Max(1, PageNumber));
            Result = await employees.SearchAdvancedAsync(ExportRequest, cancellationToken);
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در جستجوی پیشرفته");
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
        }
        return Page();
    }

    public async Task<IActionResult> OnPostExcelAsync(CancellationToken cancellationToken)
    {
        await OnPostAsync(cancellationToken);
        if (!ModelState.IsValid || ExportRequest is null) return Page();
        try
        {
            var file = await excel.ExportSearchAsync(ExportRequest, cancellationToken);
            return File(file, BulkExcelWorkbook.ContentType, "Employees_Search_Export.xlsx");
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در خروجی اکسل جستجوی پیشرفته");
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
            return Page();
        }
    }

    private async Task LoadAttributesAsync(CancellationToken cancellationToken)
    {
        Attributes = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
        // داده فقط از متادیتای SQL ساخته می شود و در HTML با فرار دادن کاراکترها تزریق می شود.
        AttributeJson = JsonSerializer.Serialize(Attributes.Select(x => new { id = x.Id, title = x.Title,
            kind = x.DataKind })).Replace("<", "\\u003C").Replace(">", "\\u003E").Replace("&", "\\u0026");
    }

}

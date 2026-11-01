using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Common;
using TIR.Application.Contracts;
using TIR.Application.Employees;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class DetailsModel(IEmployeeQueryService query, IAttributeValueService values,
    IAttributeService attributes, IFormulaService formulas, ILogger<DetailsModel> logger) : PageModel
{
    public EmployeeDetailsDto? Employee { get; private set; }
    public List<UiField> Fields { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Employee = await query.GetAsync(new GetEmployeeQuery(id, IncludeDeleted: true), cancellationToken);
        if (Employee is null) return NotFound();
        if (!Employee.IsDeleted)
        {
            Fields = (await values.ListAsync(id, true, cancellationToken))
                .Select(UiFieldFactory.FromSaved).ToList();
            await UiFieldFactory.AttachOptionsAsync(attributes, Fields, cancellationToken);
        }
        return Page();
    }

    public async Task<IActionResult> OnPostCalculateAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            await formulas.CalculateAllAsync(new FormulaCalculateAllRequest(
                id, IranDateConverter.TehranOffsetMinutes(DateTime.UtcNow)), cancellationToken);
            TempData["Success"] = "محاسبه فرمول های کارمند انجام شد.";
        }
        catch (Exception error) { logger.LogError(error, "خطا در محاسبه فرمول کارمند {Id}", id); TempData["Error"] = UiErrors.Message(error); }
        return RedirectToPage("/Employees/Details", new { id });
    }

    public string PersianDate(DateTime utc) => IranDateConverter.ToPersianDate(IranDateConverter.UtcToTehran(utc));

    public string DisplayValue(UiField field)
    {
        if (field.DataKind == 7)
            return field.BooleanIsSet || field.IsRequired ? (field.BooleanChecked ? "بله" : "خیر") : "ثبت نشده";
        if (field.DataKind == 8 && long.TryParse(field.RawValue, out var optionId))
            return field.Options.FirstOrDefault(x => x.Id == optionId)?.Title ?? field.RawValue ?? "ثبت نشده";
        return string.IsNullOrWhiteSpace(field.RawValue) ? "ثبت نشده" : field.RawValue;
    }
}

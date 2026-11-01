using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Common;
using TIR.Application.Contracts;
using TIR.Application.Employees;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class EditModel(IEmployeeQueryService query, IEmployeeService employees,
    IAttributeService attributes, IAttributeValueService values, IFormulaService formulas,
    ILogger<EditModel> logger) : PageModel
{
    [BindProperty] public FixedInput Input { get; set; } = new();
    public long Id { get; private set; }
    public List<UiField> Fields { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var employee = await query.GetAsync(new GetEmployeeQuery(id), cancellationToken);
        if (employee is null) return NotFound();
        Id = id;
        Input = new FixedInput
        {
            Id = id, EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName, LastName = employee.LastName,
            ExpectedRowVersion = Convert.ToBase64String(employee.RowVersion)
        };
        await LoadFieldsAsync(id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id, CancellationToken cancellationToken)
    {
        Id = id;
        if (!ModelState.IsValid) { await LoadFieldsAsync(id, cancellationToken); return Page(); }
        try
        {
            var version = UiFieldFactory.DecodeRowVersion(Input.ExpectedRowVersion)
                ?? throw new FormatException("نسخه کارمند ارسال نشده است.");
            await employees.UpdateAsync(new EmployeeUpdateRequest(id, version,
                Input.EmployeeCode, Input.FirstName, Input.LastName), cancellationToken);
            TempData["Success"] = "اطلاعات اصلی کارمند به روز شد.";
            return RedirectToPage("/Employees/Edit", new { id });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ویرایش کارمند {Id}", id);
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
            await LoadFieldsAsync(id, cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostValueAsync(long id, [Bind(Prefix = "Field")] UiField field,
        CancellationToken cancellationToken)
    {
        try
        {
            var definition = await attributes.GetAsync(field.AttributeId, cancellationToken);
            if (definition is null || definition.IsDeleted)
                return new JsonResult(new { success = false, message = "مشخصه فعال یافت نشد." }) { StatusCode = 422 };

            // نوع داده از دیتابیس خوانده می شود و به فرم ارسالی اعتماد نمی کنیم.
            field.Name = definition.Title;
            field.DataKind = definition.DataKind;
            field.IsRequired = definition.IsRequired;
            field.SourceKind = definition.SourceKind;
            var result = await values.SaveAsync(UiFieldFactory.ToSaveRequest(id, field), cancellationToken);
            return new JsonResult(new { success = true, message = "مقدار ذخیره شد.",
                rowVersion = Convert.ToBase64String(result.RowVersion) });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ذخیره مشخصه کارمند {Id} / {AttributeId}", id, field.AttributeId);
            return new JsonResult(new { success = false, message = UiErrors.Message(error) })
            { StatusCode = UiErrors.StatusCode(error) };
        }
    }

    public async Task<IActionResult> OnPostCalculateAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            await formulas.CalculateAllAsync(new FormulaCalculateAllRequest(id,
                IranDateConverter.TehranOffsetMinutes(DateTime.UtcNow)), cancellationToken);
            TempData["Success"] = "فرمول های کارمند محاسبه شدند.";
        }
        catch (Exception error) { logger.LogError(error, "خطا در اجرای فرمول {Id}", id); TempData["Error"] = UiErrors.Message(error); }
        return RedirectToPage("/Employees/Edit", new { id });
    }

    private async Task LoadFieldsAsync(long id, CancellationToken cancellationToken)
    {
        Fields = (await values.ListAsync(id, true, cancellationToken))
            .Select(UiFieldFactory.FromSaved).ToList();
        await UiFieldFactory.AttachOptionsAsync(attributes, Fields, cancellationToken);
    }

    public sealed class FixedInput
    {
        public long Id { get; set; }
        public string EmployeeCode { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string ExpectedRowVersion { get; set; } = "";
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Common;
using TIR.Application.Contracts;
using TIR.Application.Employees;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class DetailsModel(IEmployeeQueryService query, IEmployeeService employees,
    IAttributeValueService values, IAttributeService attributes,
    IFormulaService formulas, ILogger<DetailsModel> logger) : PageModel
{
    [BindProperty]
    public FixedInput Input { get; set; } = new();

    public EmployeeDetailsDto? Employee { get; private set; }

    public List<UiField> Fields { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        Employee = await query.GetAsync(new GetEmployeeQuery(id, IncludeDeleted: true), cancellationToken);
        if (Employee is null) return NotFound();
        Input = new FixedInput
        {
            EmployeeCode = Employee.EmployeeCode,
            FirstName = Employee.FirstName,
            LastName = Employee.LastName,
            ExpectedRowVersion = Convert.ToBase64String(Employee.RowVersion)
        };
        if (!Employee.IsDeleted)
        {
            Fields = (await values.ListAsync(id, true, cancellationToken))
                .Select(UiFieldFactory.FromSaved).ToList();
            await UiFieldFactory.AttachOptionsAsync(attributes, Fields, cancellationToken);
        }
        return Page();
    }

    /// <summary>
    /// اطلاعات ثابت فقط در صورت تغییر ذخیره می شود و نسخه ردیف کنترل می شود.
    /// </summary>
    public async Task<IActionResult> OnPostSaveFixedAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            var current = await query.GetAsync(new GetEmployeeQuery(id, IncludeDeleted: true), cancellationToken);
            if (current is null) return NotFound();
            if (current.IsDeleted)
                return new JsonResult(new { success = false, message = "کارمند حذف شده قابل ویرایش نیست." }) { StatusCode = 422 };

            var expected = UiFieldFactory.DecodeRowVersion(Input.ExpectedRowVersion);
            if (expected is null || !expected.SequenceEqual(current.RowVersion))
                return new JsonResult(new { success = false, message = "اطلاعات کارمند توسط شخص دیگری تغییر کرده است. صفحه را تازه سازی کنید." }) { StatusCode = 409 };

            if (string.IsNullOrWhiteSpace(Input.EmployeeCode) ||
                string.IsNullOrWhiteSpace(Input.FirstName) || string.IsNullOrWhiteSpace(Input.LastName))
                return new JsonResult(new { success = false, message = "کد پرسنلی، نام و نام خانوادگی الزامی هستند." }) { StatusCode = 422 };

            var changed = current.EmployeeCode != Input.EmployeeCode ||
                current.FirstName != Input.FirstName || current.LastName != Input.LastName;

            var rowVersion = current.RowVersion;
            if (changed)
            {
                // ترتیب ذخیره مهم است: ابتدا اطلاعات اصلی و سپس مشخصه های داینامیک.
                var updated = await employees.UpdateAsync(new EmployeeUpdateRequest(id, expected,
                    Input.EmployeeCode, Input.FirstName, Input.LastName), cancellationToken);
                rowVersion = updated.RowVersion;
            }

            return new JsonResult(new { success = true, changed,
                rowVersion = Convert.ToBase64String(rowVersion) });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ذخیره اطلاعات اصلی کارمند {Id}", id);
            return new JsonResult(new { success = false, message = UiErrors.Message(error) })
            { StatusCode = UiErrors.StatusCode(error) };
        }
    }

    public async Task<IActionResult> OnPostCalculateJsonAsync(long id, CancellationToken cancellationToken)
    {
        try
        {
            await formulas.CalculateAllAsync(new FormulaCalculateAllRequest(
                id, IranDateConverter.TehranOffsetMinutes(DateTime.UtcNow)), cancellationToken);
            return new JsonResult(new { success = true, message = "فرمول های کارمند محاسبه شدند." });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در محاسبه فرمول کارمند {Id}", id);
            return new JsonResult(new { success = false, message = UiErrors.Message(error) })
            { StatusCode = UiErrors.StatusCode(error) };
        }
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

    public string DataKindTitle(byte kind) => kind switch
    {
        1 => "متن", 2 => "عدد صحیح", 3 => "عدد اعشاری", 4 => "تاریخ", 5 => "ساعت",
        6 => "تاریخ و ساعت", 7 => "بولین", 8 => "انتخاب تکی", _ => "نامشخص"
    };

    public sealed class FixedInput
    {
        public string EmployeeCode { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string ExpectedRowVersion { get; set; } = "";
    }
}

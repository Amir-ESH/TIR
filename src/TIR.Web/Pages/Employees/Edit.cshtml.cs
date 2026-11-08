using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Common;
using TIR.Application.Contracts;
using TIR.Application.Employees;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class EditModel(IEmployeeQueryService query, IEmployeeService employees,
    IAttributeService attributes, IAttributeValueService values, IFormulaService formulas,
    IBulkService bulk, ILogger<EditModel> logger) : PageModel
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


    public async Task<IActionResult> OnPostAllValuesAsync(long id, CancellationToken cancellationToken)
    {
        List<UiField>? fields;
        try { fields = await Request.ReadFromJsonAsync<List<UiField>>(cancellationToken); }
        catch (System.Text.Json.JsonException)
        {
            return new JsonResult(new { success = false, message = "فرمت مقادیر ارسالی معتبر نیست." })
            { StatusCode = 400 };
        }

        if (fields is null || fields.Count == 0)
            return new JsonResult(new { success = false, message = "هیچ تغییری برای ذخیره ارسال نشده است." })
            { StatusCode = 422 };

        if (fields.Count > 2000 || fields.Select(x => x.AttributeId).Distinct().Count() != fields.Count)
            return new JsonResult(new { success = false, message = "فهرست مشخصه های ارسالی معتبر نیست." })
            { StatusCode = 422 };

        try
        {
            // نوع هر مشخصه را از دیتابیس می خوانیم و به اطلاعات مخفی فرم اعتماد نمی کنیم.
            var definitions = (await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken))
                .Where(x => !x.IsDeleted && x.SourceKind == 1)
                .ToDictionary(x => x.Id);
            var lines = new List<BulkValueInput>(fields.Count);

            foreach (var field in fields)
            {
                if (!definitions.TryGetValue(field.AttributeId, out var definition))
                    return new JsonResult(new
                    {
                        success = false,
                        message = "یکی از مشخصه های ارسالی حذف شده یا قابل ویرایش نیست."
                    }) { StatusCode = 422 };

                field.Name = definition.Title;
                field.DataKind = definition.DataKind;
                field.SourceKind = definition.SourceKind;
                field.IsRequired = definition.IsRequired;

                var value = UiFieldFactory.ToSaveRequest(id, field);
                lines.Add(new BulkValueInput(
                    value.AttributeId, value.OperationKind, value.ExpectedRowVersion,
                    value.StringValue, value.IntegerValue, value.DecimalValue,
                    value.DateValue, value.TimeValue, value.DateTimeUtc,
                    value.BooleanValue, value.OptionId));
            }

            // یک کارمند، یک واحد تراکنشی است و خطا مانع ثبت تمام تغییرات همین نوبت می شود.
            var report = await bulk.UploadAndProcessAsync(
                SingleEmployee(new BulkEmployeeInput(id, lines)), new BulkRequestOptions(),
                cancellationToken);

            // نتیجه پیش از پاکسازی خوانده شده است و گزارش می تواند توسط جاب نیز پاکسازی شود.
            try { await bulk.DeleteAsync(report.Header.BatchId, cancellationToken); }
            catch (Exception cleanupError)
            {
                logger.LogWarning(cleanupError, "پاکسازی گزارش ذخیره کلی مشخصه های کارمند {Id} انجام نشد", id);
            }

            if (report.Header.SucceededCount != 1 || report.Header.FailedCount != 0)
            {
                var errors = report.Errors.Select(error =>
                {
                    var title = error.AttributeId.HasValue &&
                                definitions.TryGetValue(error.AttributeId.Value, out var definition)
                        ? definition.Title : "مشخصه";
                    return $"{title}: {error.ErrorMessage} ({error.ErrorCode})";
                }).ToArray();
                var conflict = report.Errors.Any(x => x.ErrorCode == "ROWVERSION_CONFLICT");
                return new JsonResult(new
                {
                    success = false,
                    message = "هیچ کدام از تغییرات ذخیره نشد. خطاهای زیر را بررسی کنید.",
                    errors
                }) { StatusCode = conflict ? 409 : 422 };
            }

            TempData["Success"] = $"{lines.Count} مشخصه با موفقیت ذخیره شد.";
            return new JsonResult(new { success = true, savedCount = lines.Count });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ذخیره کلی مشخصه های کارمند {Id}", id);
            return new JsonResult(new { success = false, message = UiErrors.Message(error) })
            { StatusCode = UiErrors.StatusCode(error) };
        }
    }

    private static async IAsyncEnumerable<BulkEmployeeInput> SingleEmployee(BulkEmployeeInput employee)
    {
        yield return employee;
        await Task.CompletedTask;
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

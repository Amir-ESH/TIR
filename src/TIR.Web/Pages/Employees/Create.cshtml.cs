using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class CreateModel(IEmployeeService employees, IAttributeService attributes,
    ILogger<CreateModel> logger) : PageModel
{
    [BindProperty] public string? EmployeeCode { get; set; }
    [BindProperty] public string FirstName { get; set; } = "";
    [BindProperty] public string LastName { get; set; } = "";
    [BindProperty] public List<UiField> Fields { get; set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken) =>
        await LoadFieldsAsync(false, cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadFieldsAsync(true, cancellationToken);
        var initialValues = new List<TypedValueInput>();
        foreach (var field in Fields.Where(x => x.SourceKind == 1))
        {
            try
            {
                var parsed = UiFieldFactory.Parse(field);
                if (parsed.HasValue) initialValues.Add(parsed.Value);
                else if (field.IsRequired)
                    ModelState.AddModelError($"Fields[{Fields.IndexOf(field)}].RawValue", $"مشخصه «{field.Name}» اجباری است.");
            }
            catch (FormatException error) { ModelState.AddModelError(string.Empty, error.Message); }
        }

        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            ModelState.AddModelError(string.Empty, "نام و نام خانوادگی الزامی است.");
        if (!ModelState.IsValid) return Page();

        try
        {
            var created = await employees.CreateAsync(new EmployeeCreateRequest(EmployeeCode, FirstName,
                LastName, initialValues), cancellationToken);
            TempData["Success"] = "کارمند و اطلاعات اولیه او ثبت شدند.";
            return RedirectToPage("/Employees/Details", new { id = created.Id });
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در ایجاد کارمند");
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
            return Page();
        }
    }

    private async Task LoadFieldsAsync(bool preservePosted, CancellationToken cancellationToken)
    {
        var posted = preservePosted ? Fields.ToDictionary(x => x.AttributeId) : new Dictionary<long, UiField>();
        var definitions = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
        Fields = definitions.Select(UiFieldFactory.FromDefinition).ToList();
        foreach (var field in Fields)
        {
            if (!posted.TryGetValue(field.AttributeId, out var previous)) continue;
            field.RawValue = previous.RawValue;
            field.BooleanChecked = previous.BooleanChecked;
            field.BooleanIsSet = previous.BooleanIsSet;
        }
        await UiFieldFactory.AttachOptionsAsync(attributes, Fields, cancellationToken);
    }
}

using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TIR.Application.Common;
using TIR.Application.Contracts;
using TIR.Web.Presentation;

namespace TIR.Web.Pages.Employees;

public sealed class SearchModel(IEmployeeService employees, IAttributeService attributes,
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
            var filters = JsonSerializer.Deserialize<List<SearchCondition>>(FiltersJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
            if (filters.Count > 64) throw new FormatException("حداکثر ۶۴ شرط مجاز است.");
            var known = Attributes.ToDictionary(x => x.Id);
            var normalized = new List<SearchCondition>(filters.Count);
            foreach (var condition in filters)
            {
                if (!known.TryGetValue(condition.AttributeId, out var attribute))
                    throw new FormatException("مشخصه انتخابی نامعتبر است.");
                normalized.Add(condition with
                {
                    Value = NormalizeDate(condition.Value, attribute.DataKind),
                    To = NormalizeDate(condition.To, attribute.DataKind)
                });
            }

            Result = await employees.SearchAdvancedAsync(new AdvancedEmployeeSearchRequest(
                normalized, EmployeeSearch, SortBy, SortAttributeId, SortDescending,
                Math.Max(1, PageNumber)), cancellationToken);
        }
        catch (Exception error)
        {
            logger.LogError(error, "خطا در جستجوی پیشرفته");
            ModelState.AddModelError(string.Empty, UiErrors.Message(error));
        }
        return Page();
    }

    private async Task LoadAttributesAsync(CancellationToken cancellationToken)
    {
        Attributes = await UiFieldFactory.LoadAllActiveAsync(attributes, cancellationToken);
        // داده فقط از متادیتای SQL ساخته می شود و در HTML با فرار دادن کاراکترها تزریق می شود.
        AttributeJson = JsonSerializer.Serialize(Attributes.Select(x => new { id = x.Id, title = x.Title,
            kind = x.DataKind })).Replace("<", "\\u003C").Replace(">", "\\u003E").Replace("&", "\\u0026");
    }

    private static string? NormalizeDate(string? input, byte dataKind)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        if (dataKind == 4 && input.Contains('/'))
            return IranDateConverter.ParsePersianDate(input).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (dataKind == 6 && input.Contains('/'))
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !TimeSpan.TryParse(parts[1], CultureInfo.InvariantCulture, out var time))
                throw new FormatException("فرمت تاریخ و ساعت معتبر نیست.");
            var local = IranDateConverter.ParsePersianDate(parts[0]).Add(time);
            return IranDateConverter.TehranLocalToUtc(local).ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
        }
        return input;
    }
}

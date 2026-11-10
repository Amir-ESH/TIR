using System.Globalization;
using System.Text.Json;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Presentation;

/// <summary>
/// تبدیل مشخصه های داینامیک به ستون های دیتا گرید
/// </summary>
/// <param name="ColumnsJson">داده JSON ستون ها</param>
/// <param name="RowsJson">داده JSON ردیف ها</param>
public sealed record EmployeeGridData(string ColumnsJson, string RowsJson)
{
    public static EmployeeGridData Empty { get; } = new("[]", "[]");

    /// <summary>
    /// کارمندان، تعریف مشخصه ها و مقدار مشخصه های هر کارمند را کنار هم قرار میده
    /// و اطلاعات مناسب دیتاگرید را می سازد.
    /// </summary>
    /// <param name="page">اطلاعات ثابت کارمندان در یک صفحه</param>
    /// <param name="definitions">لیست مشخصه ها</param>
    /// <param name="valueRepository"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<EmployeeGridData> BuildAsync(
        PageResult<EmployeeListItem> page,
        IReadOnlyList<AttributeDefinitionDto> definitions,
        IEmployeeGridValueRepository valueRepository,
        CancellationToken cancellationToken)
    {
        var active = definitions.Where(x => !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArray();

        var columnsJson = JsonSerializer.Serialize(active.Select(x => new
        {
            id = x.Id,
            code = x.AttributeCode,
            title = x.Title
        }));

        var values = await valueRepository.ReadAsync(page.Items.Select(x => x.Id)
            .ToArray(), cancellationToken);
        
        var byEmployeeAndAttribute = values
            .GroupBy(x => (x.EmployeeId, x.AttributeId))
            .ToDictionary(x => x.Key, x => x.First());

        var rows = page.Items.Select(employee =>
        {
            var row = new Dictionary<string, object?>(active.Length + 5)
            {
                ["Id"] = employee.Id,
                ["EmployeeCode"] = employee.EmployeeCode,
                ["FirstName"] = employee.FirstName,
                ["LastName"] = employee.LastName,
                ["IsDeleted"] = employee.IsDeleted
            };

            foreach (var attribute in active)
            {
                byEmployeeAndAttribute.TryGetValue((employee.Id, attribute.Id), out var value);
                row["Attribute_" + attribute.Id.ToString(CultureInfo.InvariantCulture)] = Format(value, attribute.DataKind);
            }

            return row;
        });

        return new EmployeeGridData(columnsJson, JsonSerializer.Serialize(rows));
    }

    /// <summary>
    /// آماده سازی داده ها برای نمایش
    /// </summary>
    /// <param name="value">مقدار داده</param>
    /// <param name="kind">نوع داده</param>
    /// <returns></returns>
    private static string Format(EmployeeGridValueDto? value, byte kind)
    {
        if (value is null)
            return string.Empty;

        return kind switch
        {
            1 => value.StringValue ?? string.Empty,
            2 => value.IntegerValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            3 => value.DecimalValue?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            4 => value.DateValue is { } date ? IranDateConverter.ToPersianDate(date) : string.Empty,
            5 => value.TimeValue?.ToString(@"hh\:mm", CultureInfo.InvariantCulture) ?? string.Empty,
            6 => value.DateTimeUtc is { } utc ? FormatTehranDateTime(utc) : string.Empty,
            7 => value.BooleanValue is { } boolean ? (boolean ? "بله" : "خیر") : string.Empty,
            8 => value.OptionTitle ?? value.OptionId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            _ => string.Empty
        };
    }

    /// <summary>
    /// تبدیل تاریخ و ساعت میلادی وقت گرینویچ به تاریخ خورشیدی و وقت تهران
    /// </summary>
    /// <param name="utc">تاریخ و ساعت میلادی به وقت گرینویچ</param>
    /// <returns>تاریخ خورشیدی به وقت تهران</returns>
    private static string FormatTehranDateTime(DateTime utc)
    {
        var tehran = IranDateConverter.UtcToTehran(utc);

        return IranDateConverter.ToPersianDate(tehran) + " " +
            tehran.ToString("HH:mm", CultureInfo.InvariantCulture);
    }
}

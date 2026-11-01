using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Presentation;

// مدل ارائه فقط برای کنترل فرم است و جایگزین قواعد نهایی SQL نمی شود.
public sealed class UiField
{
    public long AttributeId { get; set; }
    [ValidateNever] public string Name { get; set; } = "";
    [ValidateNever] public string Code { get; set; } = "";
    public byte DataKind { get; set; }
    public byte SourceKind { get; set; }
    public bool IsRequired { get; set; }
    [ValidateNever] public string Prefix { get; set; } = "Field";
    public string? RawValue { get; set; }
    public bool BooleanChecked { get; set; }
    public bool BooleanIsSet { get; set; }
    public bool ClearValue { get; set; }
    public string? ExpectedRowVersion { get; set; }
    public byte? CalculationStatus { get; set; }
    public string? CalculationErrorMessage { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    [ValidateNever] public List<AttributeOptionDto> Options { get; set; } = [];

    public string StatusText => CalculationStatus switch
    {
        1 => "معتبر", 2 => "نیازمند محاسبه", 3 => "خطای محاسبه", _ => "محاسبه نشده"
    };

    public string StatusColor => CalculationStatus switch
    {
        1 => "success", 2 => "warning", 3 => "danger", _ => "secondary"
    };
}

public static class UiFieldFactory
{
    public static UiField FromDefinition(AttributeDefinitionDto definition) => new()
    {
        AttributeId = definition.Id,
        Code = definition.AttributeCode,
        Name = definition.Title,
        DataKind = definition.DataKind,
        SourceKind = definition.SourceKind,
        IsRequired = definition.IsRequired
    };

    public static UiField FromSaved(EmployeeFormAttributeDto source)
    {
        var field = new UiField
        {
            AttributeId = source.AttributeId,
            Code = source.AttributeCode,
            Name = source.Title,
            DataKind = source.DataKind,
            SourceKind = source.SourceKind,
            IsRequired = source.IsRequired,
            ExpectedRowVersion = source.RowVersion is null ? null : Convert.ToBase64String(source.RowVersion),
            CalculationStatus = source.CalculationStatus,
            CalculationErrorMessage = source.CalculationErrorMessage,
            ValidUntilUtc = source.ValidUntilUtc,
            BooleanIsSet = source.BooleanValue.HasValue,
            BooleanChecked = source.BooleanValue == true
        };

        field.RawValue = source.DataKind switch
        {
            1 => source.StringValue,
            2 => source.IntegerValue?.ToString(CultureInfo.InvariantCulture),
            3 => source.DecimalValue?.ToString("0.000", CultureInfo.InvariantCulture),
            4 => source.DateValue.HasValue ? IranDateConverter.ToPersianDate(source.DateValue.Value) : null,
            5 => source.TimeValue?.ToString(@"hh\:mm"),
            6 => source.DateTimeUtc.HasValue ? FormatLocalDateTime(source.DateTimeUtc.Value) : null,
            7 => source.BooleanValue?.ToString(),
            8 => source.OptionId?.ToString(CultureInfo.InvariantCulture),
            _ => null
        };
        return field;
    }

    public static async Task<List<AttributeDefinitionDto>> LoadAllActiveAsync(
        IAttributeService service, CancellationToken cancellationToken)
    {
        var all = new List<AttributeDefinitionDto>();
        for (var page = 1; ; page++)
        {
            var batch = await service.ListAsync(new AttributeListRequest(
                IncludeDeleted: false, PageNumber: page, PageSize: 200), cancellationToken);
            all.AddRange(batch.Items);
            if (all.Count >= batch.TotalCount || batch.Items.Count == 0) break;
        }
        return all;
    }

    public static async Task AttachOptionsAsync(IAttributeService service, IEnumerable<UiField> fields,
        CancellationToken cancellationToken)
    {
        foreach (var field in fields.Where(x => x.DataKind == 8 && x.SourceKind == 1))
            field.Options = (await service.ListOptionsAsync(field.AttributeId, false, cancellationToken)).ToList();
    }

    public static byte[]? DecodeRowVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return null;
        var bytes = Convert.FromBase64String(version);
        if (bytes.Length != 8) throw new FormatException("نسخه ردیف معتبر نیست. صفحه را تازه سازی کنید.");
        return bytes;
    }

    public static (bool HasValue, TypedValueInput Value) Parse(UiField field)
    {
        if (field.DataKind == 7)
        {
            var isSet = field.IsRequired || field.BooleanIsSet;
            return (isSet, new TypedValueInput(field.AttributeId,
                BooleanValue: isSet ? field.BooleanChecked : null));
        }

        var input = field.RawValue?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return (false, new TypedValueInput(field.AttributeId));

        try
        {
            var typed = field.DataKind switch
            {
                1 => new TypedValueInput(field.AttributeId, StringValue: input),
                2 => new TypedValueInput(field.AttributeId,
                    IntegerValue: long.Parse(input, CultureInfo.InvariantCulture)),
                3 => new TypedValueInput(field.AttributeId,
                    DecimalValue: decimal.Parse(input, NumberStyles.Number, CultureInfo.InvariantCulture)),
                4 => new TypedValueInput(field.AttributeId,
                    DateValue: IranDateConverter.ParsePersianDate(input)),
                5 => new TypedValueInput(field.AttributeId,
                    TimeValue: TimeSpan.Parse(input, CultureInfo.InvariantCulture)),
                6 => new TypedValueInput(field.AttributeId,
                    DateTimeUtc: ParseDateTimeUtc(input)),
                8 => new TypedValueInput(field.AttributeId,
                    OptionId: long.Parse(input, CultureInfo.InvariantCulture)),
                _ => throw new FormatException("نوع مشخصه ناشناخته است.")
            };
            return (true, typed);
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            throw new FormatException($"مقدار مشخصه «{field.Name}» معتبر نیست.", exception);
        }
    }

    public static ValueSaveRequest ToSaveRequest(long employeeId, UiField field)
    {
        var version = DecodeRowVersion(field.ExpectedRowVersion);
        var parsed = Parse(field);
        var clear = field.ClearValue || !parsed.HasValue;
        if (clear)
            return new ValueSaveRequest(employeeId, field.AttributeId, version, 2);

        var value = parsed.Value;
        return new ValueSaveRequest(employeeId, field.AttributeId, version, 1,
            value.StringValue, value.IntegerValue, value.DecimalValue,
            value.DateValue, value.TimeValue, value.DateTimeUtc,
            value.BooleanValue, value.OptionId);
    }

    private static string FormatLocalDateTime(DateTime utc)
    {
        var local = IranDateConverter.UtcToTehran(utc);
        return $"{IranDateConverter.ToPersianDate(local)} {local:HH:mm}";
    }

    private static DateTime ParseDateTimeUtc(string input)
    {
        var pieces = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length != 2 || !TimeSpan.TryParse(pieces[1], CultureInfo.InvariantCulture, out var clock))
            throw new FormatException("قالب تاریخ و زمان معتبر نیست.");
        var day = IranDateConverter.ParsePersianDate(pieces[0]);
        return IranDateConverter.TehranLocalToUtc(day.Add(clock));
    }
}

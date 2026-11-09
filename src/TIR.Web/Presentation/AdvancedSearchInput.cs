using System.Globalization;
using System.Text.Json;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Presentation;

/// <summary>
/// فرمت تاریخ شروط جستجو در صفحه جستجو و انتخاب کارمندان یکسان است.
/// </summary>
public static class AdvancedSearchInput
{
    public static IReadOnlyList<SearchCondition> Parse(string json,
        IReadOnlyList<AttributeDefinitionDto> attributes)
    {
        var conditions = JsonSerializer.Deserialize<List<SearchCondition>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        if (conditions.Count > 64)
            throw new FormatException("حداکثر 64 شرط مجاز است.");

        var known = attributes.ToDictionary(x => x.Id);

        return conditions.Select(condition =>
        {
            if (!known.TryGetValue(condition.AttributeId, out var attribute))
                throw new FormatException("مشخصه انتخابی معتبر نیست.");

            var isValuePresenceFilter = condition.Operator is "isnull" or "isnotnull";

            if (isValuePresenceFilter && attribute.IsRequired)
                throw new FormatException("بررسی خالی بودن فقط برای مشخصه غیر اجباری مجاز است.");

            return condition with
            {
                Value = isValuePresenceFilter ? null : NormalizeDate(condition.Value, attribute.DataKind),
                To = isValuePresenceFilter ? null : NormalizeDate(condition.To, attribute.DataKind)
            };
        }).ToArray();
    }

    private static string? NormalizeDate(string? input, byte dataKind)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        if (dataKind == 4 && input.Contains('/'))
            return IranDateConverter.ParsePersianDate(input).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (dataKind == 6 && input.Contains('/'))
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2 || !TimeSpan.TryParse(parts[1], CultureInfo.InvariantCulture, out var time))
                throw new FormatException("قالب تاریخ و ساعت معتبر نیست.");

            var local = IranDateConverter.ParsePersianDate(parts[0]).Add(time);

            return IranDateConverter.TehranLocalToUtc(local)
                .ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);
        }

        return input;
    }
}

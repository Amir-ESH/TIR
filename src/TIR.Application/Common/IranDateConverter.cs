using System.Globalization;

namespace TIR.Application.Common;

/// <summary>
/// این کلاس فقط تبدیل ورودی و نمایش تاریخ را انجام می دهد؛ صحت نهایی در SQL است.
/// </summary>
public static class IranDateConverter
{
    private static readonly PersianCalendar Calendar = new();

    public static TimeZoneInfo TehranTimeZone { get; } = LoadTehranTimeZone();

    public static string ToPersianDate(DateTime gregorianDate)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0:0000}/{1:00}/{2:00}",
            Calendar.GetYear(gregorianDate), Calendar.GetMonth(gregorianDate),
            Calendar.GetDayOfMonth(gregorianDate));
    }

    public static DateTime ParsePersianDate(string persianDate)
    {
        if (string.IsNullOrWhiteSpace(persianDate))
            throw new FormatException("تاریخ شمسی خالی است.");

        var normalized = NormalizeDigits(persianDate.Trim()).Replace('-', '/');
        var parts = normalized.Split('/');
        if (parts.Length != 3 ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day))
            throw new FormatException("قالب تاریخ شمسی باید سال/ماه/روز باشد.");
        return Calendar.ToDateTime(year, month, day, 0, 0, 0, 0);
    }

    public static DateTime TehranLocalToUtc(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        if (TehranTimeZone.IsInvalidTime(unspecified) || TehranTimeZone.IsAmbiguousTime(unspecified))
            throw new ArgumentException("زمان محلی مبهم یا نامعتبر است.");
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, TehranTimeZone);
    }

    public static DateTime UtcToTehran(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, TehranTimeZone);
    }

    public static short TehranOffsetMinutes(DateTime utcNow)
    {
        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        return checked((short)TehranTimeZone.GetUtcOffset(utc).TotalMinutes);
    }

    private static TimeZoneInfo LoadTehranTimeZone()
    {
        foreach (var id in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        throw new TimeZoneNotFoundException("منطقه زمانی ایران روی سرور موجود نیست.");
    }

    private static string NormalizeDigits(string input)
    {
        var result = new char[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            var digit = input[i];
            result[i] = digit is >= '۰' and <= '۹'
                ? (char)('0' + digit - '۰')
                : digit is >= '٠' and <= '٩'
                    ? (char)('0' + digit - '٠')
                    : digit;
        }
        return new string(result);
    }
}

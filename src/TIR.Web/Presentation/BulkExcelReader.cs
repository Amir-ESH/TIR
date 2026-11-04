using System.Globalization;
using ClosedXML.Excel;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Presentation;

public sealed record BulkExcelEmployeeRow(
    int ExcelRow, long? EmployeeId, byte[]? ExpectedEmployeeRowVersion,
    string? EmployeeCode, string? FirstName, string? LastName,
    IReadOnlyList<BulkValueInput> Values);

/// <summary>
/// قرارداد خواندن دقیقا همان قرارداد ایجاد فایل نمونه و فایل خروجی است.
/// </summary>
public static class BulkExcelReader
{
    public static IReadOnlyList<BulkExcelEmployeeRow> Read(Stream stream, bool update,
        IReadOnlyList<AttributeDefinitionDto> attributes)
    {
        using var workbook = new XLWorkbook(stream);
        if (!workbook.Worksheets.TryGetWorksheet("Employees", out var sheet) || sheet is null)
            throw new FormatException("برگه Employees در اکسل وجود ندارد.");

        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in sheet.Row(1).CellsUsed())
        {
            var name = cell.GetString().Trim();
            if (name.Length == 0 || !columns.TryAdd(name, cell.Address.ColumnNumber))
                throw new FormatException("عنوان ستون ها تکراری یا خالی است.");
        }
        var requiredColumns = update
            ? new[] { "EmployeeId", "EmployeeRowVersion", "EmployeeCode", "FirstName", "LastName" }
            : new[] { "EmployeeCode", "FirstName", "LastName" };
        foreach (var name in requiredColumns)
            if (!columns.ContainsKey(name))
                throw new FormatException($"ستون {name} در فایل وجود ندارد.");

        var inputAttributes = attributes.Where(x => !x.IsDeleted && x.SourceKind == 1).ToArray();
        var validCodes = new HashSet<string>(requiredColumns, StringComparer.OrdinalIgnoreCase);
        foreach (var attribute in inputAttributes)
        {
            validCodes.Add(attribute.AttributeCode);
            if (update)
                validCodes.Add(attribute.AttributeCode + "__RowVersion");
        }
        // ستون فرمولی ممکن است در نمونه های قدیمی وجود داشته باشد؛ محتوای آن نباید پردازش شود.
        var ignoredFormulaColumns = new HashSet<string>(
            attributes.Where(x => !x.IsDeleted && x.SourceKind == 2).Select(x => x.AttributeCode),
            StringComparer.OrdinalIgnoreCase);
        validCodes.UnionWith(ignoredFormulaColumns);
        if (columns.Keys.Any(x => !validCodes.Contains(x)))
            throw new FormatException("فایل دارای ستون ناشناخته است. نمونه جدید دریافت کنید.");

        var effectiveColumnPositions = columns.Where(x => !ignoredFormulaColumns.Contains(x.Key))
            .Select(x => x.Value).ToArray();

        // اگر فایل از خروجی کارکنان تولید شده باشد، فقط مشخصه های تغییر کرده ارسال می شوند.
        // فایل نمونه بدون برگه مرجع، همه خانه ها را به عنوان مقدار جدید یا دستور پاک کردن می پذیرد.
        var originals = new Dictionary<long, Dictionary<string, string>>();
        if (update && workbook.Worksheets.TryGetWorksheet("OriginalValues", out var baseline) && baseline is not null)
        {
            var headers = baseline.Row(1).CellsUsed().ToDictionary(
                x => x.GetString().Trim(), x => x.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
            if (!headers.TryGetValue("EmployeeId", out var keyColumn))
                throw new FormatException("برگه OriginalValues فاقد شناسه کارمند است.");
            foreach (var baseRow in baseline.RowsUsed().Skip(1))
            {
                var id = ParseId(baseRow.Cell(keyColumn).GetString().Trim(), baseRow.RowNumber());
                if (!id.HasValue) continue;
                var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var attribute in inputAttributes)
                    if (headers.TryGetValue(attribute.AttributeCode, out var position))
                        snapshot[attribute.AttributeCode] = baseRow.Cell(position).GetString().Trim();
                if (!originals.TryAdd(id.Value, snapshot))
                    throw new FormatException("شناسه کارمند در برگه OriginalValues تکراری است.");
            }
        }

        // ردیف های اضافه که فقط مقدار ستون فرمولی دارند، ردیف داده محسوب نمی شوند.
        var last = effectiveColumnPositions.Select(column =>
            sheet.Column(column).LastCellUsed()?.Address.RowNumber ?? 1).DefaultIfEmpty(1).Max();
        var output = new List<BulkExcelEmployeeRow>(Math.Max(0, last - 1));
        for (var number = 2; number <= last; number++)
        {
            var row = sheet.Row(number);
            if (effectiveColumnPositions.All(column => row.Cell(column).IsEmpty())) continue;
            string? Get(string code) => columns.TryGetValue(code, out var col)
                ? row.Cell(col).GetFormattedString().Trim() : null;

            var id = ParseId(Get("EmployeeId"), number);
            var employeeVersion = Decode(Get("EmployeeRowVersion"), number);
            if (update && (id is null || employeeVersion is null))
                throw new FormatException($"ردیف {number}: شناسه کارمند و نسخه آن الزامی است.");
            var values = new List<BulkValueInput>();
            var original = id.HasValue && originals.TryGetValue(id.Value, out var stored) ? stored : null;
            foreach (var attribute in inputAttributes)
            {
                if (!columns.ContainsKey(attribute.AttributeCode)) continue;
                var raw = Get(attribute.AttributeCode) ?? "";
                if (original is not null && original.TryGetValue(attribute.AttributeCode, out var oldRaw)
                    && string.Equals(raw, oldRaw, StringComparison.Ordinal))
                    continue;
                var version = update ? Decode(Get(attribute.AttributeCode + "__RowVersion"), number) : null;
                if (!update && raw.Length == 0) continue;
                var operation = raw.Length == 0 ? (byte)2 : (byte)1;
                try
                {
                    values.Add(Parse(attribute, operation, version, raw));
                }
                catch (Exception error) when (error is FormatException or OverflowException or ArgumentOutOfRangeException)
                {
                    throw new FormatException($"ردیف {number}، مشخصه {attribute.Title}: {error.Message}", error);
                }
            }
            output.Add(new BulkExcelEmployeeRow(number, id, employeeVersion,
                Get("EmployeeCode"), Get("FirstName"), Get("LastName"), values));
        }
        if (output.Count == 0) throw new FormatException("فایل اکسل هیچ کارمندی ندارد.");
        return output;
    }

    private static BulkValueInput Parse(AttributeDefinitionDto attribute, byte operation,
        byte[]? version, string raw)
    {
        var id = attribute.Id;
        if (operation == 2) return new BulkValueInput(id, 2, version);
        return attribute.DataKind switch
        {
            1 => new(id, 1, version, StringValue: raw),
            2 => new(id, 1, version, IntegerValue: long.Parse(raw, CultureInfo.InvariantCulture)),
            3 => new(id, 1, version, DecimalValue: decimal.Parse(raw, CultureInfo.InvariantCulture)),
            4 => new(id, 1, version, DateValue: IranDateConverter.ParsePersianDate(raw)),
            5 => new(id, 1, version, TimeValue: TimeSpan.ParseExact(raw, @"hh\:mm", CultureInfo.InvariantCulture)),
            6 => new(id, 1, version, DateTimeUtc: ParseDateTime(raw)),
            7 => new(id, 1, version, BooleanValue: raw switch
            {
                "1" or "true" or "True" => true,
                "0" or "false" or "False" => false,
                _ => throw new FormatException("مقدار بولین باید صفر یا یک باشد.")
            }),
            8 => new(id, 1, version, OptionId: long.Parse(raw, CultureInfo.InvariantCulture)),
            _ => throw new FormatException("نوع مشخصه قابل ورود نیست.")
        };
    }

    private static DateTime ParseDateTime(string raw)
    {
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new FormatException("تاریخ و ساعت باید به صورت 1405/08/12 14:30 باشد.");
        return IranDateConverter.TehranLocalToUtc(IranDateConverter.ParsePersianDate(parts[0]) +
            TimeSpan.ParseExact(parts[1], @"hh\:mm", CultureInfo.InvariantCulture));
    }

    private static long? ParseId(string? raw, int line)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) && id > 0)
            return id;
        throw new FormatException($"ردیف {line}: شناسه کارمند معتبر نیست.");
    }

    private static byte[]? Decode(string? raw, int line)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try
        {
            var decoded = Convert.FromBase64String(raw);
            if (decoded.Length == 8) return decoded;
        }
        catch (FormatException) { }
        throw new FormatException($"ردیف {line}: نسخه داده باید هشت بایت Base64 باشد.");
    }
}

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Presentation;

/// <summary>
/// ورودی CSV به ترتیب شناسه کارمند است؛ هر کارمند یک واحد مستقل Bulk می شود.
/// </summary>
public static class BulkCsvReader
{
    public static async IAsyncEnumerable<BulkEmployeeInput> ReadAsync(Stream stream,
        IReadOnlyDictionary<long, byte> attributeKinds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false),
            detectEncodingFromByteOrderMarks: true, bufferSize: 64 * 1024, leaveOpen: true);

        long? currentEmployee = null;
        List<BulkValueInput> currentValues = [];
        var hasCurrent = false;
        var lineNumber = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            var columns = SplitCsvLine(line);

            if (lineNumber == 1 && columns.Count > 0 &&
                columns[0].Trim().Equals("EmployeeId", StringComparison.OrdinalIgnoreCase))
                continue;

            if (columns.Count != 5)
                throw new FormatException($"خط {lineNumber}: هر ردیف باید دقیقا 5 ستون داشته باشد.");

            var employeeId = long.TryParse(columns[0], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var parsedEmployee) ? parsedEmployee : (long?)null;

            var attributeId = long.TryParse(columns[1], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var parsedAttribute) ? parsedAttribute : (long?)null;

            var operation = byte.TryParse(columns[2], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var parsedOperation) ? parsedOperation : (byte?)null;

            if (hasCurrent && currentEmployee != employeeId)
            {
                yield return new BulkEmployeeInput(currentEmployee, currentValues);
                currentValues = [];
            }

            currentEmployee = employeeId;
            hasCurrent = true;

            byte[]? version = null;

            if (!string.IsNullOrWhiteSpace(columns[4]))
            {
                try
                {
                    var decoded = Convert.FromBase64String(columns[4]);

                    if (decoded.Length == 8)
                        version = decoded;
                }
                catch (FormatException) { }
            }

            var raw = columns[3].Trim();
            currentValues.Add(ParseValue(attributeId, operation, version, raw, attributeKinds));
        }

        if (hasCurrent)
            yield return new BulkEmployeeInput(currentEmployee, currentValues);
    }

    private static BulkValueInput ParseValue(long? attributeId, byte? operation, byte[]? version,
        string raw, IReadOnlyDictionary<long, byte> kinds)
    {
        if (operation == 2)
            return new BulkValueInput(attributeId, operation, version);

        if (attributeId is null || !kinds.TryGetValue(attributeId.Value, out var kind))
            return new BulkValueInput(attributeId, operation, version, StringValue: raw);

        try
        {
            return kind switch
            {
                1 => new BulkValueInput(attributeId, operation, version, StringValue: raw),
                2 => new BulkValueInput(attributeId, operation, version,
                    IntegerValue: long.Parse(raw, CultureInfo.InvariantCulture)),
                3 => new BulkValueInput(attributeId, operation, version,
                    DecimalValue: decimal.Parse(raw, NumberStyles.Number, CultureInfo.InvariantCulture)),
                4 => new BulkValueInput(attributeId, operation, version,
                    DateValue: raw.Contains('/') ? IranDateConverter.ParsePersianDate(raw)
                        : DateTime.ParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture)),
                5 => new BulkValueInput(attributeId, operation, version,
                    TimeValue: TimeSpan.Parse(raw, CultureInfo.InvariantCulture)),
                6 => new BulkValueInput(attributeId, operation, version,
                    DateTimeUtc: ParseDateTime(raw)),
                7 => new BulkValueInput(attributeId, operation, version,
                    BooleanValue: raw is "1" or "true" or "True" ? true
                        : raw is "0" or "false" or "False" ? false : throw new FormatException()),
                8 => new BulkValueInput(attributeId, operation, version,
                    OptionId: long.Parse(raw, CultureInfo.InvariantCulture)),
                _ => new BulkValueInput(attributeId, operation, version, StringValue: raw)
            };
        }
        catch (Exception error) when (error is FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            return new BulkValueInput(attributeId, operation, version, StringValue: raw);
        }
    }

    private static DateTime ParseDateTime(string raw)
    {
        if (raw.Contains('/'))
        {
            var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                throw new FormatException();

            return IranDateConverter.TehranLocalToUtc(
                IranDateConverter.ParsePersianDate(parts[0]).Add(TimeSpan.Parse(parts[1], CultureInfo.InvariantCulture)));
        }
        return DateTime.Parse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var buffer = new StringBuilder();
        var quoted = false;

        for (var i = 0; i < line.Length; i++)
        {
            var current = line[i];

            if (current == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                { 
                    buffer.Append('"');
                    i++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (current == ',' && !quoted)
            {
                fields.Add(buffer.ToString());
                buffer.Clear();
            }
            else
            {
                buffer.Append(current);
            }
        }

        if (quoted)
            throw new FormatException("علامت نقل قول CSV کامل نشده است.");

        fields.Add(buffer.ToString());

        return fields;
    }
}

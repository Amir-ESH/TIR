using System.Globalization;
using ClosedXML.Excel;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Web.Presentation;

/// <summary>
/// تمام فایل های ورود و خروج اکسل از یک قرارداد ستون استفاده می کنند.
/// </summary>
public static class BulkExcelWorkbook
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const int MaximumRows = 50000;

    public static byte[] Template(bool update, IReadOnlyList<AttributeDefinitionDto> attributes,
        IReadOnlyList<AttributeOptionDto> options)
    {
        using var workbook = CreateWorkbook(update, attributes, options, out _, out _);
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] Export(bool update, IReadOnlyList<AttributeDefinitionDto> attributes,
        IReadOnlyList<EmployeeListItem> employees,
        IReadOnlyDictionary<long, IReadOnlyList<EmployeeFormAttributeDto>> values,
        IReadOnlyList<AttributeOptionDto> options)
    {
        if (employees.Count > MaximumRows)
            throw new InvalidOperationException($"حداکثر {MaximumRows:N0} کارمند در هر فایل مجاز است.");

        using var workbook = CreateWorkbook(update, attributes, options, out var data, out var columns);
        var editable = attributes.Where(x => !x.IsDeleted && x.SourceKind == 1)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToArray();
        // مقدارهای اولیه برای تشخیص ستون های تغییر کرده در خروجی ویرایش نگهداری می شوند.
        var baseline = update ? workbook.Worksheets.Add("OriginalValues") : null;
        if (baseline is not null)
        {
            baseline.Cell(1, 1).Value = "EmployeeId";
            for (var index = 0; index < editable.Length; index++)
                baseline.Cell(1, index + 2).Value = editable[index].AttributeCode;
            baseline.Columns(1, editable.Length + 1).Style.NumberFormat.Format = "@";
        }
        var row = 2;
        foreach (var employee in employees)
        {
            var col = 1;
            if (update)
            {
                data.Cell(row, col++).Value = employee.Id.ToString(CultureInfo.InvariantCulture);
                data.Cell(row, col++).Value = Convert.ToBase64String(employee.RowVersion);
                baseline!.Cell(row, 1).Value = employee.Id.ToString(CultureInfo.InvariantCulture);
            }
            data.Cell(row, col++).Value = employee.EmployeeCode;
            data.Cell(row, col++).Value = employee.FirstName ?? "";
            data.Cell(row, col++).Value = employee.LastName ?? "";
            Dictionary<long, EmployeeFormAttributeDto> lookup = values.TryGetValue(employee.Id, out var items)
                ? items.ToDictionary(x => x.AttributeId) : new();
            for (var index = 0; index < editable.Length; index++)
            {
                var attribute = editable[index];
                lookup.TryGetValue(attribute.Id, out var value);
                var raw = Format(value, attribute.DataKind);
                data.Cell(row, col++).Value = raw;
                if (baseline is not null) baseline.Cell(row, index + 2).Value = raw;
                if (update)
                    data.Cell(row, col++).Value = value?.RowVersion is { Length: 8 }
                        ? Convert.ToBase64String(value.RowVersion) : "";
            }
            row++;
        }

        // شناسه و نسخه را متن نگه می داریم تا اکسل ارقام طولانی را تغییر ندهد.
        data.Columns(1, columns).Style.NumberFormat.Format = "@";
        data.Range(1, 1, Math.Max(1, row - 1), columns).SetAutoFilter();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static XLWorkbook CreateWorkbook(bool update,
        IReadOnlyList<AttributeDefinitionDto> attributes, IReadOnlyList<AttributeOptionDto> options,
        out IXLWorksheet data, out int columnCount)
    {
        var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Employees");
        data = sheet;
        sheet.RightToLeft = true;
        sheet.ShowGridLines = false;
        var guide = workbook.Worksheets.Add("راهنما");
        guide.RightToLeft = true;
        guide.ShowGridLines = false;

        var column = 1;
        if (update)
        {
            AddHeader("EmployeeId");
            AddHeader("EmployeeRowVersion");
        }
        AddHeader("EmployeeCode");
        AddHeader("FirstName");
        AddHeader("LastName");

        // مشخصه های فرمولی فقط توسط موتور فرمول مقداردهی می شوند و ستون ورودی ندارند.
        var inputAttributes = attributes.Where(x => !x.IsDeleted && x.SourceKind == 1)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToList();
        foreach (var attribute in inputAttributes)
        {
            AddHeader(attribute.AttributeCode);
            if (update) AddHeader(attribute.AttributeCode + "__RowVersion");
        }
        columnCount = column - 1;
        data.Range(1, 1, 1, columnCount).Style.Fill.BackgroundColor = XLColor.FromHtml("#243553");
        data.Range(1, 1, 1, columnCount).Style.Font.FontColor = XLColor.White;
        data.Range(1, 1, 1, columnCount).Style.Font.Bold = true;
        data.Row(1).Height = 28;
        data.SheetView.FreezeRows(1);

        guide.Cell(1, 1).Value = "ستون";
        guide.Cell(1, 2).Value = "عنوان فارسی";
        guide.Cell(1, 3).Value = "فرمت";
        guide.Cell(1, 4).Value = "الزامی";
        var guideRow = 2;
        foreach (var attribute in inputAttributes)
        {
            guide.Cell(guideRow, 1).Value = attribute.AttributeCode;
            guide.Cell(guideRow, 2).Value = attribute.Title;
            guide.Cell(guideRow, 3).Value = TypeHelp(attribute.DataKind);
            guide.Cell(guideRow, 4).Value = attribute.IsRequired ? "بله" : "خیر";
            guideRow++;
        }
        guide.Cell(guideRow++, 1).Value = "مشخصه های فرمولی در فایل اکسل ورودی قرار نمی گیرند و با اجرای فرمول محاسبه می شوند.";
        guide.Cell(guideRow++, 1).Value = "تاریخ خورشیدی به صورت 1405/08/12 و تاریخ و ساعت به صورت 1405/08/12 14:30 وارد شوند.";
        if (update)
        {
            guide.Cell(guideRow++, 1).Value = "در ویرایش، خانه خالی مقدار موجود را پاک می کند.";
            guide.Cell(guideRow++, 1).Value = "ستون های RowVersion را تغییر ندهید. نسخه نامعتبر باعث خطای تعارض می شود.";
            guide.Cell(guideRow, 1).Value = "در خروجی ویرایش برگه OriginalValues مبنای تشخیص تغییرات است و نباید تغییر کند.";
        }
        guide.Columns(1, 4).AdjustToContents();
        guide.Column(1).Width = Math.Max(guide.Column(1).Width, 36);
        guide.Column(2).Width = Math.Max(guide.Column(2).Width, 22);
        guide.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#243553");
        guide.Range(1, 1, 1, 4).Style.Font.FontColor = XLColor.White;

        // گزینه های انتخابی همراه با شناسه واقعی برای ورود در ستون مشخصه نمایش داده می شوند.
        var optionSheet = workbook.Worksheets.Add("گزینه ها");
        optionSheet.RightToLeft = true;
        optionSheet.Cell(1, 1).Value = "کد مشخصه";
        optionSheet.Cell(1, 2).Value = "نام مشخصه";
        optionSheet.Cell(1, 3).Value = "شناسه گزینه";
        optionSheet.Cell(1, 4).Value = "عنوان گزینه";
        var optionRow = 2;
        var attributesById = inputAttributes.ToDictionary(x => x.Id);
        foreach (var option in options.Where(x => x.IsEnabled && attributesById.ContainsKey(x.AttributeId)))
        {
            var attribute = attributesById[option.AttributeId];
            optionSheet.Cell(optionRow, 1).Value = attribute.AttributeCode;
            optionSheet.Cell(optionRow, 2).Value = attribute.Title;
            optionSheet.Cell(optionRow, 3).Value = option.Id.ToString(CultureInfo.InvariantCulture);
            optionSheet.Cell(optionRow, 4).Value = option.Title;
            optionRow++;
        }
        optionSheet.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#243553");
        optionSheet.Range(1, 1, 1, 4).Style.Font.FontColor = XLColor.White;
        optionSheet.Columns(1, 4).AdjustToContents();
        return workbook;

        void AddHeader(string name)
        {
            sheet.Cell(1, column).Value = name;
            sheet.Column(column).Width = name.EndsWith("RowVersion", StringComparison.Ordinal) ? 25 : 24;
            sheet.Column(column).Style.NumberFormat.Format = "@";
            column++;
        }
    }

    private static string Format(EmployeeFormAttributeDto? value, byte kind)
    {
        if (value is null) return "";
        return kind switch
        {
            1 => value.StringValue ?? "",
            2 => value.IntegerValue?.ToString(CultureInfo.InvariantCulture) ?? "",
            3 => value.DecimalValue?.ToString(CultureInfo.InvariantCulture) ?? "",
            4 => value.DateValue is { } date ? IranDateConverter.ToPersianDate(date) : "",
            5 => value.TimeValue?.ToString(@"hh\:mm", CultureInfo.InvariantCulture) ?? "",
            6 => value.DateTimeUtc is { } utc ?
                IranDateConverter.ToPersianDate(IranDateConverter.UtcToTehran(utc)) + " " +
                IranDateConverter.UtcToTehran(utc).ToString("HH:mm", CultureInfo.InvariantCulture) : "",
            7 => value.BooleanValue is null ? "" : value.BooleanValue.Value ? "1" : "0",
            8 => value.OptionId?.ToString(CultureInfo.InvariantCulture) ?? "",
            _ => ""
        };
    }

    private static string TypeHelp(byte kind) => kind switch
    {
        1 => "متن", 2 => "عدد صحیح", 3 => "عدد اعشاری", 4 => "تاریخ شمسی 1405/08/12",
        5 => "ساعت HH:mm", 6 => "تاریخ و ساعت شمسی", 7 => "بولین 0 یا 1", 8 => "شناسه گزینه فعال", _ => ""
    };
}

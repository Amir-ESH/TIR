namespace TIR.Application.Contracts;

/// <summary>
/// تمام زمان های دارای پسوند Utc در خروجی SQL به وقت UTC هستند.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="TotalCount"></param>
/// <param name="Items"></param>
public sealed record PageResult<T>(long TotalCount, IReadOnlyList<T> Items);

/// <summary>
/// اطلاعات ثابت یک کارمند
/// </summary>
public sealed class EmployeeListItem
{
    /// <summary>
    /// شناسه کارمند
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// کد پرسنلی
    /// </summary>
    public string EmployeeCode { get; set; } = string.Empty;
    
    /// <summary>
    /// نام کارمند
    /// </summary>
    public string? FirstName { get; set; }
    
    /// <summary>
    /// نام خانوادگی کارمند
    /// </summary>
    public string? LastName { get; set; }
    
    /// <summary>
    /// آیا حذف شده؟
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// تاریخ حذف به وقت گرینویچ
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
    
    /// <summary>
    /// نسخه ردیف
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
    
    /// <summary>
    /// آیا نیازمند محاسبه فرمول دوباره است؟
    /// </summary>
    public bool HasStaleFormula { get; set; }
}

/// <summary>
/// مشخصه داینامیک
/// </summary>
public sealed class AttributeDefinitionDto
{
    /// <summary>
    /// شناسه مشخصه داینامیک
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// کد مشخصه داینامیک
    /// </summary>
    public string AttributeCode { get; set; } = string.Empty;
    
    /// <summary>
    /// عنوان فارسی مشخصه داینامیک
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// توضیح مشخصه داینامیک
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// منبع ورود داده از طریق ورود کاربر یا فرمول
    /// </summary>
    public byte SourceKind { get; set; }
    
    /// <summary>
    /// نوع دیتای ورودی
    /// </summary>
    public byte DataKind { get; set; }
    
    /// <summary>
    /// آیا اجباری هست؟
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// ترتیب نمایش
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// آیا حذف شده؟
    /// </summary>
    public bool IsDeleted { get; set; }
    
    /// <summary>
    /// تاریخ حذف
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
    
    /// <summary>
    /// تاریخ ایجاد
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
    
    /// <summary>
    /// تاریخ آخرین بروزرسانی
    /// </summary>
    public DateTime? UpdatedAtUtc { get; set; }
    
    /// <summary>
    /// نسخه ردیف
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// گزینه مشخصه انتخابی
/// </summary>
public sealed class AttributeOptionDto
{
    /// <summary>
    /// شناسه گزینه
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// شناسه مشخصه
    /// </summary>
    public long AttributeId { get; set; }
    
    /// <summary>
    /// کد گزینه
    /// </summary>
    public string OptionCode { get; set; } = string.Empty;
    
    /// <summary>
    /// عنوان فارسی گزینه
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// ترتیب نمایش در دراپ داون
    /// </summary>
    public int DisplayOrder { get; set; }
    
    /// <summary>
    /// وضعیت فعال بودن
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// تاریخ ایجاد
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
    
    /// <summary>
    /// نسخه بروزرسانی
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// مقدار فیلد مشخصه داینامیک
/// </summary>
public sealed class EmployeeAttributeValueDto
{
    /// <summary>
    /// شناسه
    /// </summary>
    public long Id { get; set; }
    
    /// <summary>
    /// شناسه کارمند
    /// </summary>
    public long EmployeeId { get; set; }
    
    /// <summary>
    /// شناسه مشخصه
    /// </summary>
    public long AttributeId { get; set; }
    
    /// <summary>
    /// مقدار متنی
    /// </summary>
    public string? StringValue { get; set; }
    
    /// <summary>
    /// مقدار عدد صحیح
    /// </summary>
    public long? IntegerValue { get; set; }
    
    /// <summary>
    /// مقدار عدد اعشاری
    /// </summary>
    public decimal? DecimalValue { get; set; }
    
    /// <summary>
    /// مقدار تاریخ
    /// </summary>
    public DateTime? DateValue { get; set; }
    
    /// <summary>
    /// مقدار ساعت
    /// </summary>
    public TimeSpan? TimeValue { get; set; }
    
    /// <summary>
    /// مقدار تاریخ و ساعت
    /// </summary>
    public DateTime? DateTimeUtc { get; set; }
    
    /// <summary>
    /// مقدار منطقی
    /// </summary>
    public bool? BooleanValue { get; set; }
    
    /// <summary>
    /// شناسه گزینه انتخابی
    /// </summary>
    public long? OptionId { get; set; }
    
    /// <summary>
    /// وضعیت محاسبه فرمولی
    /// </summary>
    public byte? CalculationStatus { get; set; }
    
    /// <summary>
    /// تاریخ محاسبه فرمولی
    /// </summary>
    public DateTime? CalculatedAtUtc { get; set; }
    
    /// <summary>
    /// تاریخ اعتبار محاسبه فرمولی
    /// </summary>
    public DateTime? ValidUntilUtc { get; set; }
    
    /// <summary>
    /// نسخه فرمول محاسبه کننده
    /// </summary>
    public int? FormulaRevisionApplied { get; set; }
    
    /// <summary>
    /// کد خطای محاسبه
    /// </summary>
    public string? CalculationErrorCode { get; set; }
    
    /// <summary>
    /// متن خطای محاسبه
    /// </summary>
    public string? CalculationErrorMessage { get; set; }
    
    /// <summary>
    /// عنوان
    /// </summary>
    public string? Title { get; set; }
    
    /// <summary>
    /// نوع ورودی
    /// </summary>
    public byte SourceKind { get; set; }
    
    /// <summary>
    /// نوع مقدار
    /// </summary>
    public byte DataKind { get; set; }
    
    /// <summary>
    /// آیا اجباری هست
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// نسخه ردیف
    /// </summary>
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// فهرست فرم شامل مشخصه هایی است که هنوز برای آنها مقدار ثبت نشده است.
/// </summary>
public sealed class EmployeeFormAttributeDto
{
    public long EmployeeId { get; set; }
    
    public long AttributeId { get; set; }
    
    public string AttributeCode { get; set; } = string.Empty;
    
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public byte SourceKind { get; set; }
    
    public byte DataKind { get; set; }
    
    public bool IsRequired { get; set; }
    
    public int DisplayOrder { get; set; }
    
    public long? ValueId { get; set; }
    
    public string? StringValue { get; set; }
    
    public long? IntegerValue { get; set; }
    
    public decimal? DecimalValue { get; set; }
    
    public DateTime? DateValue { get; set; }
    
    public TimeSpan? TimeValue { get; set; }
    
    public DateTime? DateTimeUtc { get; set; }
    
    public bool? BooleanValue { get; set; }
    
    public long? OptionId { get; set; }
    
    public byte? CalculationStatus { get; set; }
    
    public DateTime? CalculatedAtUtc { get; set; }
    
    public DateTime? ValidUntilUtc { get; set; }
    
    public string? CalculationErrorCode { get; set; }
    
    public string? CalculationErrorMessage { get; set; }
    
    public byte[]? RowVersion { get; set; }
}

public sealed class EmployeeGridValueDto
{
    public long EmployeeId { get; set; }
    
    public long AttributeId { get; set; }
    
    public string? StringValue { get; set; }
    
    public long? IntegerValue { get; set; }
    
    public decimal? DecimalValue { get; set; }
    
    public DateTime? DateValue { get; set; }
    
    public TimeSpan? TimeValue { get; set; }
    
    public DateTime? DateTimeUtc { get; set; }
    
    public bool? BooleanValue { get; set; }
    
    public long? OptionId { get; set; }
    
    public string? OptionTitle { get; set; }
}

public sealed class ChangeStateDto
{
    public long Id { get; set; }
    
    public byte[] RowVersion { get; set; } = [];
    
    public bool IsDeleted { get; set; }
    
    public DateTime? DeletedAtUtc { get; set; }
}

public sealed class ValueSaveResultDto
{
    public long Id { get; set; }
    
    public long EmployeeId { get; set; }
    
    public long AttributeId { get; set; }
    
    public byte[] RowVersion { get; set; } = [];
    
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class AttributeFormulaDto
{
    public long Id { get; set; }
    
    public long AttributeId { get; set; }
    
    public string ExecutorName { get; set; } = string.Empty;
    
    public string ExecutorType { get; set; } = string.Empty;
    
    public int Revision { get; set; }
    
    public byte ValidityPolicy { get; set; }
    
    public bool IsEnabled { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    
    public DateTime? UpdatedAtUtc { get; set; }
    
    public byte[] RowVersion { get; set; } = [];
}

public sealed class FormulaDependencyDto
{
    public long Id { get; set; }
    
    public long FormulaId { get; set; }
    
    public long DependsOnAttributeId { get; set; }
    
    public byte[] RowVersion { get; set; } = [];
}

public sealed class FormulaOrderDto
{
    public long FormulaId { get; set; }
    
    public long AttributeId { get; set; }
    
    public int CalculationOrder { get; set; }
}

public sealed class FormulaCalculationDto
{
    public long Id { get; set; }
    
    public long EmployeeId { get; set; }
    
    public long AttributeId { get; set; }
    
    public byte CalculationStatus { get; set; }
    
    public DateTime? CalculatedAtUtc { get; set; }
    
    public DateTime? ValidUntilUtc { get; set; }
    
    public byte[] RowVersion { get; set; } = [];
}

public sealed class FormulaSummaryDto
{
    public long AttributeId { get; set; }
    
    public byte? CalculationStatus { get; set; }
    
    public DateTime? ValidUntilUtc { get; set; }
    
    public byte[] RowVersion { get; set; } = [];
}

public sealed class BulkReportHeaderDto
{
    public Guid BatchId { get; set; }
    
    public byte Status { get; set; }
    
    public int RequestedCount { get; set; }
    
    public int SucceededCount { get; set; }
    
    public int FailedCount { get; set; }
    
    public DateTime? StartedAtUtc { get; set; }
    
    public DateTime? FinishedAtUtc { get; set; }
    
    public string? FailureCode { get; set; }
}

public sealed class BulkEmployeeResultDto
{
    public int InputOrdinal { get; set; }
    
    public long? EmployeeId { get; set; }
    
    public byte Status { get; set; }
    
    public DateTime ProcessedAtUtc { get; set; }
}

public sealed class BulkErrorDto
{
    public Guid BatchId { get; set; }
    
    public int InputOrdinal { get; set; }
    
    public int? LineOrdinal { get; set; }
    
    public long? EmployeeId { get; set; }
    
    public long? AttributeId { get; set; }
    
    public string ErrorCode { get; set; } = string.Empty;
    
    public string ErrorMessage { get; set; } = string.Empty;
    
    public byte Status { get; set; }
}

/// <summary>
/// 
/// </summary>
/// <param name="Header"></param>
/// <param name="Employees"></param>
/// <param name="Errors"></param>
public sealed record BulkReportDto(BulkReportHeaderDto Header,
    IReadOnlyList<BulkEmployeeResultDto> Employees,
    IReadOnlyList<BulkErrorDto> Errors);

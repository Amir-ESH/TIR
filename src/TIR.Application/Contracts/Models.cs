namespace TIR.Application.Contracts;

/// <summary>
/// تمام زمان های دارای پسوند Utc در خروجی SQL به وقت UTC هستند.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="TotalCount"></param>
/// <param name="Items"></param>
public sealed record PageResult<T>(long TotalCount, IReadOnlyList<T> Items);

public sealed class EmployeeListItem
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public bool HasStaleFormula { get; set; }
}

public sealed class AttributeDefinitionDto
{
    public long Id { get; set; }
    public string AttributeCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte SourceKind { get; set; }
    public byte DataKind { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class AttributeOptionDto
{
    public long Id { get; set; }
    public long AttributeId { get; set; }
    public string OptionCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

public sealed class EmployeeAttributeValueDto
{
    public long Id { get; set; }
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
    public byte? CalculationStatus { get; set; }
    public DateTime? CalculatedAtUtc { get; set; }
    public DateTime? ValidUntilUtc { get; set; }
    public int? FormulaRevisionApplied { get; set; }
    public string? CalculationErrorCode { get; set; }
    public string? CalculationErrorMessage { get; set; }
    public string? Title { get; set; }
    public byte SourceKind { get; set; }
    public byte DataKind { get; set; }
    public bool IsRequired { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>
/// فهرست فرم شامل مشخصه هایی است که هنوز برای آنها مقدار ثبت نشده است.
/// </summary>
public sealed class EmployeeFormAttributeDto
{
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

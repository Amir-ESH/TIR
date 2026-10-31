namespace TIR.Application.Contracts;

public sealed record EmployeeCreateRequest(
    string? EmployeeCode, string FirstName, string LastName,
    IReadOnlyList<TypedValueInput>? InitialValues = null);

public sealed record EmployeeUpdateRequest(
    long Id, byte[] ExpectedRowVersion,
    string EmployeeCode, string FirstName, string LastName);

public sealed record StateChangeRequest(long Id, byte[] ExpectedRowVersion);

public sealed record EmployeeListRequest(
    string? Search = null, bool IncludeDeleted = false,
    string SortBy = "Id", bool SortDescending = false,
    int PageNumber = 1, int PageSize = 50);

/// <summary>
/// گروه های مختلف با OR و شرط های هم گروه با AND به هم متصل می شوند.
/// </summary>
/// <param name="Group"></param>
/// <param name="AttributeId"></param>
/// <param name="Operator"></param>
/// <param name="Value"></param>
/// <param name="To"></param>
public sealed record SearchCondition(
    int Group, long AttributeId, string Operator, string? Value, string? To = null);

public sealed record AdvancedEmployeeSearchRequest(
    IReadOnlyList<SearchCondition> Filters, string? EmployeeSearch = null,
    string SortBy = "Id", long? SortAttributeId = null,
    bool SortDescending = false, int PageNumber = 1, int PageSize = 50);

public sealed record AttributeCreateRequest(
    string AttributeCode, string Title, string? Description,
    byte SourceKind, byte DataKind, bool IsRequired, int DisplayOrder);

public sealed record AttributeUpdateRequest(
    long Id, byte[] ExpectedRowVersion, string Title,
    string? Description, bool IsRequired, int DisplayOrder);

public sealed record AttributeListRequest(
    string? Search = null, bool IncludeDeleted = false,
    int PageNumber = 1, int PageSize = 50);

public sealed record AttributeOptionCreateRequest(
    long AttributeId, string OptionCode, string Title, int DisplayOrder = 0);

/// <summary>
/// مقدار هر ورودی تنها در یک ستون نوع دار قرار می گیرد.
/// </summary>
/// <param name="AttributeId"></param>
/// <param name="StringValue"></param>
/// <param name="IntegerValue"></param>
/// <param name="DecimalValue"></param>
/// <param name="DateValue"></param>
/// <param name="TimeValue"></param>
/// <param name="DateTimeUtc"></param>
/// <param name="BooleanValue"></param>
/// <param name="OptionId"></param>
public sealed record TypedValueInput(
    long AttributeId,
    string? StringValue = null, long? IntegerValue = null,
    decimal? DecimalValue = null, DateTime? DateValue = null,
    TimeSpan? TimeValue = null, DateTime? DateTimeUtc = null,
    bool? BooleanValue = null, long? OptionId = null);

/// <summary>
/// عملیات ۱ ذخیره مقدار و عملیات ۲ خالی کردن مقدار است.
/// </summary>
/// <param name="EmployeeId"></param>
/// <param name="AttributeId"></param>
/// <param name="ExpectedRowVersion"></param>
/// <param name="OperationKind"></param>
/// <param name="StringValue"></param>
/// <param name="IntegerValue"></param>
/// <param name="DecimalValue"></param>
/// <param name="DateValue"></param>
/// <param name="TimeValue"></param>
/// <param name="DateTimeUtc"></param>
/// <param name="BooleanValue"></param>
/// <param name="OptionId"></param>
public sealed record ValueSaveRequest(
    long EmployeeId, long AttributeId,
    byte[]? ExpectedRowVersion, byte OperationKind,
    string? StringValue = null, long? IntegerValue = null,
    decimal? DecimalValue = null, DateTime? DateValue = null,
    TimeSpan? TimeValue = null, DateTime? DateTimeUtc = null,
    bool? BooleanValue = null, long? OptionId = null);

public sealed record FormulaRegisterRequest(
    long AttributeId, string ExecutorName,
    string ExecutorType, byte ValidityPolicy,
    byte[]? ExpectedRowVersion = null);

public sealed record FormulaDependencyAddRequest(
    long FormulaId, long DependsOnAttributeId);

public sealed record FormulaCalculateRequest(
    long EmployeeId, long FormulaId, short UtcOffsetMinutes);

public sealed record FormulaCalculateAllRequest(long EmployeeId, short UtcOffsetMinutes);

public sealed record FormulaCalculateSetRequest(
    long FormulaId, short UtcOffsetMinutes,
    int MaxEmployees = 2000, bool RecalculateValid = false);

/// <summary>
/// هر کارمند واحد مستقل اعتبارسنجی و موفقیت یا شکست در SQL است.
/// </summary>
/// <param name="AttributeId"></param>
/// <param name="OperationKind"></param>
/// <param name="ExpectedRowVersion"></param>
/// <param name="StringValue"></param>
/// <param name="IntegerValue"></param>
/// <param name="DecimalValue"></param>
/// <param name="DateValue"></param>
/// <param name="TimeValue"></param>
/// <param name="DateTimeUtc"></param>
/// <param name="BooleanValue"></param>
/// <param name="OptionId"></param>
public sealed record BulkValueInput(
    long? AttributeId, byte? OperationKind,
    byte[]? ExpectedRowVersion,
    string? StringValue = null, long? IntegerValue = null,
    decimal? DecimalValue = null, DateTime? DateValue = null,
    TimeSpan? TimeValue = null, DateTime? DateTimeUtc = null,
    bool? BooleanValue = null, long? OptionId = null);

public sealed record BulkEmployeeInput(
    long? EmployeeId, IReadOnlyList<BulkValueInput> Values);

public sealed record BulkRequestOptions(
    int TransferBatchSize = 3000,
    int EmployeesPerTransaction = 2000,
    Guid? IdempotencyKey = null);

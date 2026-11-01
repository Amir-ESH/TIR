namespace TIR.Application.Contracts;

/// <summary>
/// 
/// </summary>
/// <param name="EmployeeCode">کد کارمند</param>
/// <param name="FirstName">نام</param>
/// <param name="LastName">نام خانوادگی</param>
/// <param name="InitialValues"></param>
public sealed record EmployeeCreateRequest(
    string? EmployeeCode, string FirstName, string LastName,
    IReadOnlyList<TypedValueInput>? InitialValues = null);

/// <summary>
/// 
/// </summary>
/// <param name="Id">شناسه</param>
/// <param name="ExpectedRowVersion">نسخه خط کارمند</param>
/// <param name="EmployeeCode">کد کارمند</param>
/// <param name="FirstName">نام</param>
/// <param name="LastName">نام خانوادگی</param>
public sealed record EmployeeUpdateRequest(
    long Id, byte[] ExpectedRowVersion,
    string EmployeeCode, string FirstName, string LastName);

/// <summary>
/// 
/// </summary>
/// <param name="Id">شناسه</param>
/// <param name="ExpectedRowVersion">نسخه خط مورد انتظار</param>
public sealed record StateChangeRequest(long Id, byte[] ExpectedRowVersion);

/// <summary>
/// 
/// </summary>
/// <param name="Search">جستجو</param>
/// <param name="IncludeDeleted">نمایش حذف شده ها</param>
/// <param name="SortBy">مرتب سازی بر اساس</param>
/// <param name="SortDescending">مرتب سازی نزولی</param>
/// <param name="PageNumber">شماره صفحه</param>
/// <param name="PageSize">تعداد در هر صفحه</param>
public sealed record EmployeeListRequest(
    string? Search = null, bool IncludeDeleted = false,
    string SortBy = "Id", bool SortDescending = false,
    int PageNumber = 1, int PageSize = 50);

/// <summary>
/// گروه های مختلف با OR و شرط های هم گروه با AND به هم متصل می شوند.
/// </summary>
/// <param name="Group">گروه</param>
/// <param name="AttributeId">شناسه مشخصه</param>
/// <param name="Operator">عملگر</param>
/// <param name="Value">مقدار</param>
/// <param name="To">تا</param>
public sealed record SearchCondition(
    int Group, long AttributeId, string Operator, string? Value, string? To = null);

/// <summary>
/// 
/// </summary>
/// <param name="Filters">فیلتر ها</param>
/// <param name="EmployeeSearch">جستجوی کارمند</param>
/// <param name="SortBy">مرتب سازی بر اساس</param>
/// <param name="SortAttributeId">مرتب سازی بر اساس شناسه مشخصه</param>
/// <param name="SortDescending">مرتب سازی نزولی</param>
/// <param name="PageNumber">شماره صفحه</param>
/// <param name="PageSize">تعداد در هر صفحه</param>
public sealed record AdvancedEmployeeSearchRequest(
    IReadOnlyList<SearchCondition> Filters, string? EmployeeSearch = null,
    string SortBy = "Id", long? SortAttributeId = null,
    bool SortDescending = false, int PageNumber = 1, int PageSize = 50);

/// <summary>
/// 
/// </summary>
/// <param name="AttributeCode">کد مشخصه</param>
/// <param name="Title">عنوان مشخصه</param>
/// <param name="Description">توضیح مشخصه</param>
/// <param name="SourceKind">منبع ورود داده</param>
/// <param name="DataKind">نوع داده ورودی</param>
/// <param name="IsRequired">آیا اجباری است</param>
/// <param name="DisplayOrder">ترتیب نمایش</param>
public sealed record AttributeCreateRequest(
    string AttributeCode, string Title, string? Description,
    byte SourceKind, byte DataKind, bool IsRequired, int DisplayOrder);

/// <summary>
/// 
/// </summary>
/// <param name="Id">شناسه</param>
/// <param name="ExpectedRowVersion">نسخه خط مورد انتظار</param>
/// <param name="Title">عنوان</param>
/// <param name="Description">توضیح</param>
/// <param name="IsRequired">آیا اجباری است؟</param>
/// <param name="DisplayOrder">ترتیب نمایش</param>
public sealed record AttributeUpdateRequest(
    long Id, byte[] ExpectedRowVersion, string Title,
    string? Description, bool IsRequired, int DisplayOrder);

/// <summary>
/// 
/// </summary>
/// <param name="Search">جستجو</param>
/// <param name="IncludeDeleted">نمایش حذف شده ها</param>
/// <param name="PageNumber">شماره صفحه</param>
/// <param name="PageSize">تعداد در صفحه</param>
public sealed record AttributeListRequest(
    string? Search = null, bool IncludeDeleted = false,
    int PageNumber = 1, int PageSize = 50);

/// <summary>
/// 
/// </summary>
/// <param name="AttributeId">شناسه مشخصه</param>
/// <param name="OptionCode"></param>
/// <param name="Title">عنوان</param>
/// <param name="DisplayOrder">ترتیب نمایش</param>
public sealed record AttributeOptionCreateRequest(
    long AttributeId, string OptionCode, string Title, int DisplayOrder = 0);

/// <summary>ویرایش عنوان و ترتیب گزینه بدون تغییر کد ثابت آن.</summary>
public sealed record AttributeOptionUpdateRequest(
    long AttributeId, long Id, byte[] ExpectedRowVersion, string Title, int DisplayOrder);

/// <summary>تغییر وضعیت فعال بودن گزینه با کنترل نسخه.</summary>
public sealed record AttributeOptionStateRequest(
    long AttributeId, long Id, byte[] ExpectedRowVersion, bool IsEnabled);

/// <summary>
/// مقدار هر ورودی تنها در یک ستون نوع دار قرار می گیرد.
/// </summary>
/// <param name="AttributeId">شناسه مشخصه</param>
/// <param name="StringValue">مقدار متنی</param>
/// <param name="IntegerValue">مقدار عدد صحیح</param>
/// <param name="DecimalValue">مقدار عدد اعشار</param>
/// <param name="DateValue">مقدار تاریخ</param>
/// <param name="TimeValue">مقدار زمان</param>
/// <param name="DateTimeUtc">مقدار تاریخ به گرینویچ</param>
/// <param name="BooleanValue">مقدار بولین</param>
/// <param name="OptionId">شناسه مقدار انتخابی</param>
public sealed record TypedValueInput(
    long AttributeId,
    string? StringValue = null, long? IntegerValue = null,
    decimal? DecimalValue = null, DateTime? DateValue = null,
    TimeSpan? TimeValue = null, DateTime? DateTimeUtc = null,
    bool? BooleanValue = null, long? OptionId = null);

/// <summary>
/// عملیات ۱ ذخیره مقدار و عملیات ۲ خالی کردن مقدار است.
/// </summary>
/// <param name="EmployeeId">شناسه کارمند</param>
/// <param name="AttributeId">شناسه مشخصه</param>
/// <param name="ExpectedRowVersion">نسخه خط</param>
/// <param name="OperationKind">نوع عملیات</param>
/// <param name="StringValue">مقدار متنی</param>
/// <param name="IntegerValue">مقدار عدد صحیح</param>
/// <param name="DecimalValue">مقدار عدد اعشار</param>
/// <param name="DateValue">مقدار تاریخ</param>
/// <param name="TimeValue">مقدار زمان</param>
/// <param name="DateTimeUtc">مقدار تاریخ به گرینویچ</param>
/// <param name="BooleanValue">مقدار بولین</param>
/// <param name="OptionId">شناسه مقدار انتخابی</param>
public sealed record ValueSaveRequest(
    long EmployeeId, long AttributeId,
    byte[]? ExpectedRowVersion, byte OperationKind,
    string? StringValue = null, long? IntegerValue = null,
    decimal? DecimalValue = null, DateTime? DateValue = null,
    TimeSpan? TimeValue = null, DateTime? DateTimeUtc = null,
    bool? BooleanValue = null, long? OptionId = null);

/// <summary>
/// 
/// </summary>
/// <param name="AttributeId">شناسه مشخصه</param>
/// <param name="ExecutorName">نام اجرا کننده</param>
/// <param name="ExecutorType">نوع اجرا کننده</param>
/// <param name="ValidityPolicy">سیاست اعتبار سنجی</param>
/// <param name="ExpectedRowVersion">نسخه خط مورد انتظار</param>
public sealed record FormulaRegisterRequest(
    long AttributeId, string ExecutorName,
    string ExecutorType, byte ValidityPolicy,
    byte[]? ExpectedRowVersion = null);

/// <summary>
/// 
/// </summary>
/// <param name="FormulaId">شناسه فرمول</param>
/// <param name="DependsOnAttributeId">شناسه مشخصه مرتبط</param>
public sealed record FormulaDependencyAddRequest(
    long FormulaId, long DependsOnAttributeId);

/// <summary>
/// 
/// </summary>
/// <param name="EmployeeId">شناسه کارمند</param>
/// <param name="FormulaId">شناسه فرمول</param>
/// <param name="UtcOffsetMinutes"></param>
public sealed record FormulaCalculateRequest(
    long EmployeeId, long FormulaId, short UtcOffsetMinutes);

/// <summary>
/// 
/// </summary>
/// <param name="EmployeeId">شناسه کارمند</param>
/// <param name="UtcOffsetMinutes"></param>
public sealed record FormulaCalculateAllRequest(long EmployeeId, short UtcOffsetMinutes);

/// <summary>
/// 
/// </summary>
/// <param name="FormulaId">شناسه فرمول</param>
/// <param name="UtcOffsetMinutes"></param>
/// <param name="MaxEmployees">حداکثر کارمند</param>
/// <param name="RecalculateValid"></param>
public sealed record FormulaCalculateSetRequest(
    long FormulaId, short UtcOffsetMinutes,
    int MaxEmployees = 2000, bool RecalculateValid = false);

/// <summary>
/// هر کارمند واحد مستقل اعتبارسنجی و موفقیت یا شکست در SQL است.
/// </summary>
/// <param name="AttributeId">شناسه مشخصه</param>
/// <param name="ExpectedRowVersion">نسخه خط</param>
/// <param name="OperationKind">نوع عملیات</param>
/// <param name="StringValue">مقدار متنی</param>
/// <param name="IntegerValue">مقدار عدد صحیح</param>
/// <param name="DecimalValue">مقدار عدد اعشار</param>
/// <param name="DateValue">مقدار تاریخ</param>
/// <param name="TimeValue">مقدار زمان</param>
/// <param name="DateTimeUtc">مقدار تاریخ به گرینویچ</param>
/// <param name="BooleanValue">مقدار بولین</param>
/// <param name="OptionId">شناسه مقدار انتخابی</param>
public sealed record BulkValueInput(
    long? AttributeId, byte? OperationKind,
    byte[]? ExpectedRowVersion,
    string? StringValue = null, long? IntegerValue = null,
    decimal? DecimalValue = null, DateTime? DateValue = null,
    TimeSpan? TimeValue = null, DateTime? DateTimeUtc = null,
    bool? BooleanValue = null, long? OptionId = null);

/// <summary>
/// 
/// </summary>
/// <param name="EmployeeId">شناسه کارمند</param>
/// <param name="Values">مقادیر</param>
public sealed record BulkEmployeeInput(long? EmployeeId, IReadOnlyList<BulkValueInput> Values);

/// <summary>
/// 
/// </summary>
/// <param name="TransferBatchSize"></param>
/// <param name="EmployeesPerTransaction"></param>
/// <param name="IdempotencyKey"></param>
public sealed record BulkRequestOptions(
    int TransferBatchSize = 3000,
    int EmployeesPerTransaction = 2000,
    Guid? IdempotencyKey = null);

namespace TIR.Application.Common;

/// <summary>
/// انواع خطای دیتابیس
/// </summary>
public enum DatabaseFailureKind
{
    /// <summary>
    /// خطای بیزینسی
    /// </summary>
    Business,

    /// <summary>
    /// خطای همزمانی
    /// </summary>
    Concurrency,

    /// <summary>
    /// داپلیکیت
    /// </summary>
    Duplicate,

    /// <summary>
    /// خطای موقت
    /// </summary>
    Transient,

    /// <summary>
    /// غیرقابل پیشبینی
    /// </summary>
    Unexpected
}

/// <summary>
/// اطلاعات مورد نیاز Presentation برای نمایش تعارض و خطای تجاری.
/// </summary>
/// <param name="message">پیام</param>
/// <param name="errorNumber">شماره خطا</param>
/// <param name="kind">نوع خطای دیتابیس</param>
/// <param name="innerException">استثنای داخلی</param>
public sealed class DatabaseFailureException(
    string message, int errorNumber, DatabaseFailureKind kind, Exception innerException)
    : Exception(message, innerException)
{
    public int ErrorNumber { get; } = errorNumber;
    public DatabaseFailureKind Kind { get; } = kind;
}

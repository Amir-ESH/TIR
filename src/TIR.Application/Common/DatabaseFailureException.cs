namespace TIR.Application.Common;

public enum DatabaseFailureKind
{
    Business,
    Concurrency,
    Duplicate,
    Transient,
    Unexpected
}

/// <summary>
/// اطلاعات مورد نیاز Presentation برای نمایش تعارض و خطای تجاری.
/// </summary>
/// <param name="message"></param>
/// <param name="errorNumber"></param>
/// <param name="kind"></param>
/// <param name="innerException"></param>
public sealed class DatabaseFailureException(
    string message, int errorNumber, DatabaseFailureKind kind, Exception innerException)
    : Exception(message, innerException)
{
    public int ErrorNumber { get; } = errorNumber;
    public DatabaseFailureKind Kind { get; } = kind;
}

namespace TIR.Application.Employees;

/// <summary>
/// خروجی اطلاعات ثابت کارمند برای لایه ارائه.
/// </summary>
public sealed record EmployeeDetailsDto(
    long Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    bool IsDeleted,
    DateTime? DeletedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    byte[] RowVersion);

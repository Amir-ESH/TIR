namespace TIR.Domain.Employees;

/// <summary>
/// تصویر اطلاعات ثابت کارمند طبق خروجی پایگاه داده.
/// </summary>
public sealed record Employee(
    long Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    bool IsDeleted,
    DateTime? DeletedAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    byte[] RowVersion);

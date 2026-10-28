namespace TIR.Application.Employees;

/// <summary>
/// درخواست دریافت اطلاعات ثابت کارمند.
/// </summary>
public sealed record GetEmployeeQuery(long Id, bool IncludeDeleted = false);

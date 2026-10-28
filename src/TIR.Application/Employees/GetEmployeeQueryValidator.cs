using FluentValidation;

namespace TIR.Application.Employees;

/// <summary>
/// کنترل اولیه شناسه ورودی پیش از فراخوانی SQL.
/// </summary>
public sealed class GetEmployeeQueryValidator : AbstractValidator<GetEmployeeQuery>
{
    public GetEmployeeQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEqual(0)
            .WithMessage("شناسه کارمند نباید صفر باشد.");
    }
}

using FluentValidation;

namespace TIR.Application.Contracts;

internal static class InputRules
{
    public static bool HasVersion(byte[]? value) => value is { Length: 8 };
    public static bool HasOptionalVersion(byte[]? value) => value is null || HasVersion(value);
    public static bool ValidPage(int number, int size) => number > 0 && size is >= 1 and <= 200;
}

public sealed class EmployeeCreateRequestValidator : AbstractValidator<EmployeeCreateRequest>
{
    public EmployeeCreateRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
        RuleForEach(x => x.InitialValues).SetValidator(new TypedValueInputValidator());
    }
}

public sealed class EmployeeUpdateRequestValidator : AbstractValidator<EmployeeUpdateRequest>
{
    public EmployeeUpdateRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ExpectedRowVersion).Must(InputRules.HasVersion).WithMessage("نسخه هشت بایتی کارمند الزامی است.");
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50);
    }
}

public sealed class StateChangeRequestValidator : AbstractValidator<StateChangeRequest>
{
    public StateChangeRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ExpectedRowVersion).Must(InputRules.HasVersion).WithMessage("نسخه هشت بایتی الزامی است.");
    }
}

public sealed class EmployeeListRequestValidator : AbstractValidator<EmployeeListRequest>
{
    public EmployeeListRequestValidator()
    {
        RuleFor(x => x).Must(x => InputRules.ValidPage(x.PageNumber, x.PageSize));
        RuleFor(x => x.SortBy).Must(x => new[] { "Id", "EmployeeCode", "FirstName", "LastName" }.Contains(x));
        RuleFor(x => x.Search).MaximumLength(150);
    }
}

public sealed class AdvancedEmployeeSearchRequestValidator : AbstractValidator<AdvancedEmployeeSearchRequest>
{
    public AdvancedEmployeeSearchRequestValidator()
    {
        RuleFor(x => x).Must(x => InputRules.ValidPage(x.PageNumber, x.PageSize));
        RuleFor(x => x.SortBy).Must(x => new[] { "Id", "EmployeeCode", "FirstName", "LastName" }.Contains(x));
        RuleFor(x => x.SortAttributeId).Must(x => x is null or > 0);
        RuleFor(x => x.EmployeeSearch).MaximumLength(150);
        RuleFor(x => x.Filters).NotNull().Must(x => x is { Count: <= 64 });
        RuleForEach(x => x.Filters).ChildRules(filter =>
        {
            filter.RuleFor(x => x.Group).InclusiveBetween(1, 16);
            filter.RuleFor(x => x.AttributeId).GreaterThan(0);
            filter.RuleFor(x => x.Operator)
                .Must(x => new[] { "eq", "ne", "gt", "gte", "lt", "lte", "between", "contains" }.Contains(x));
            filter.RuleFor(x => x.Value).NotNull().MaximumLength(1000);
            filter.RuleFor(x => x.To).MaximumLength(1000);
        });
    }
}

public sealed class AttributeCreateRequestValidator : AbstractValidator<AttributeCreateRequest>
{
    public AttributeCreateRequestValidator()
    {
        RuleFor(x => x.AttributeCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.SourceKind).InclusiveBetween((byte)1, (byte)2);
        RuleFor(x => x.DataKind).InclusiveBetween((byte)1, (byte)8);
    }
}

public sealed class AttributeUpdateRequestValidator : AbstractValidator<AttributeUpdateRequest>
{
    public AttributeUpdateRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ExpectedRowVersion).Must(InputRules.HasVersion);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public sealed class AttributeListRequestValidator : AbstractValidator<AttributeListRequest>
{
    public AttributeListRequestValidator()
    {
        RuleFor(x => x).Must(x => InputRules.ValidPage(x.PageNumber, x.PageSize));
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public sealed class AttributeOptionCreateRequestValidator : AbstractValidator<AttributeOptionCreateRequest>
{
    public AttributeOptionCreateRequestValidator()
    {
        RuleFor(x => x.AttributeId).GreaterThan(0);
        RuleFor(x => x.OptionCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public sealed class TypedValueInputValidator : AbstractValidator<TypedValueInput>
{
    public TypedValueInputValidator()
    {
        RuleFor(x => x.AttributeId).GreaterThan(0);
        RuleFor(x => x.StringValue).MaximumLength(1000);
    }
}

public sealed class ValueSaveRequestValidator : AbstractValidator<ValueSaveRequest>
{
    public ValueSaveRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.AttributeId).GreaterThan(0);
        RuleFor(x => x.ExpectedRowVersion).Must(InputRules.HasOptionalVersion);
        RuleFor(x => x.OperationKind).Must(x => x is 1 or 2);
        RuleFor(x => x.StringValue).MaximumLength(1000);
    }
}

public sealed class FormulaRegisterRequestValidator : AbstractValidator<FormulaRegisterRequest>
{
    public FormulaRegisterRequestValidator()
    {
        RuleFor(x => x.AttributeId).GreaterThan(0);
        RuleFor(x => x.ExecutorName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ExecutorType).Must(x => new[] { "P", "FN", "IF", "TF" }.Contains(x));
        RuleFor(x => x.ValidityPolicy).InclusiveBetween((byte)1, (byte)3);
        RuleFor(x => x.ExpectedRowVersion).Must(InputRules.HasOptionalVersion);
    }
}

public sealed class FormulaDependencyAddRequestValidator : AbstractValidator<FormulaDependencyAddRequest>
{
    public FormulaDependencyAddRequestValidator()
    {
        RuleFor(x => x.FormulaId).GreaterThan(0);
        RuleFor(x => x.DependsOnAttributeId).GreaterThan(0);
    }
}

public sealed class FormulaCalculateRequestValidator : AbstractValidator<FormulaCalculateRequest>
{
    public FormulaCalculateRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.FormulaId).GreaterThan(0);
        RuleFor(x => x.UtcOffsetMinutes).InclusiveBetween((short)-840, (short)840);
    }
}

public sealed class FormulaCalculateAllRequestValidator : AbstractValidator<FormulaCalculateAllRequest>
{
    public FormulaCalculateAllRequestValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.UtcOffsetMinutes).InclusiveBetween((short)-840, (short)840);
    }
}

public sealed class FormulaCalculateSetRequestValidator : AbstractValidator<FormulaCalculateSetRequest>
{
    public FormulaCalculateSetRequestValidator()
    {
        RuleFor(x => x.FormulaId).GreaterThan(0);
        RuleFor(x => x.UtcOffsetMinutes).InclusiveBetween((short)-840, (short)840);
        RuleFor(x => x.MaxEmployees).InclusiveBetween(1, 10000);
    }
}

public sealed class BulkRequestOptionsValidator : AbstractValidator<BulkRequestOptions>
{
    public BulkRequestOptionsValidator()
    {
        RuleFor(x => x.TransferBatchSize).InclusiveBetween(100, 10000);
        RuleFor(x => x.EmployeesPerTransaction).InclusiveBetween(100, 10000);
    }
}

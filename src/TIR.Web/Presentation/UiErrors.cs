using FluentValidation;
using TIR.Application.Common;

namespace TIR.Web.Presentation;

public static class UiErrors
{
    public static string Message(Exception error) => error switch
    {
        DatabaseFailureException database => database.Message,
        ValidationException validation => string.Join("، ", validation.Errors.Select(x => x.ErrorMessage)),
        FormatException => error.Message,
        ArgumentException => error.Message,
        _ => "در انجام درخواست خطای غیرمنتظره رخ داد. دوباره تلاش کنید."
    };

    public static int StatusCode(Exception error) => error switch
    {
        DatabaseFailureException { Kind: DatabaseFailureKind.Concurrency } => 409,
        DatabaseFailureException { Kind: DatabaseFailureKind.Duplicate } => 409,
        DatabaseFailureException { Kind: DatabaseFailureKind.Business } => 422,
        FormatException or ArgumentException or ValidationException => 400,
        _ => 500
    };
}

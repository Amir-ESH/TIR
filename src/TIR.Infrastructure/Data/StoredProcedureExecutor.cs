using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using TIR.Application.Common;
using TIR.Application.Contracts;

namespace TIR.Infrastructure.Data;

/// <summary>
/// این اجرا کننده هیچ منطق تجاری ندارد و فقط  SP ها را اجرا می کند.
/// </summary>
/// <param name="connectionFactory"></param>
internal sealed class StoredProcedureExecutor(SqlConnectionFactory connectionFactory)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="timeoutSeconds"></param>
    /// <param name="transaction"></param>
    /// <returns></returns>
    public static CommandDefinition Command(string name, object? parameters,
        CancellationToken cancellationToken, int timeoutSeconds = 90, IDbTransaction? transaction = null) =>
        new(name, parameters, transaction: transaction,
            commandTimeout: timeoutSeconds, commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<T?> SingleOrDefaultAsync<T>(string name, object? parameters,
        CancellationToken cancellationToken) =>
        WithConnectionAsync(connection =>
            connection.QuerySingleOrDefaultAsync<T>(Command(name, parameters, cancellationToken)),
            true, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="timeoutSeconds"></param>
    /// <returns></returns>
    public Task<T> SingleAsync<T>(string name, object? parameters,
        CancellationToken cancellationToken, int timeoutSeconds = 90) =>
        WithConnectionAsync(connection =>
            connection.QuerySingleAsync<T>(Command(name, parameters, cancellationToken, timeoutSeconds)),
            false, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<IReadOnlyList<T>> ListAsync<T>(string name, object? parameters,
        CancellationToken cancellationToken) =>
        WithConnectionAsync<IReadOnlyList<T>>(async connection =>
            (await connection.QueryAsync<T>(Command(name, parameters, cancellationToken))).ToList(),
            true, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<PageResult<T>> PageAsync<T>(string name, object? parameters,
        CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            using var result = await connection.QueryMultipleAsync(Command(name, parameters, cancellationToken));
            var total = await result.ReadSingleAsync<long>();
            var items = (await result.ReadAsync<T>()).ToList();
            return new PageResult<T>(total, items);
        }, true, cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="parameters"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="timeoutSeconds"></param>
    /// <returns></returns>
    public Task ExecuteAsync(string name, object? parameters, CancellationToken cancellationToken,
        int timeoutSeconds = 90) =>
        WithConnectionAsync(async connection =>
        {
            await connection.ExecuteAsync(Command(name, parameters, cancellationToken, timeoutSeconds));

            return 0;
        }, false, cancellationToken);

    /// <summary>
    /// تلاش دوباره فقط روی خواندن بی اثر در صورت Deadlock انجام می شود.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="operation"></param>
    /// <param name="readOnly"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TResult> WithConnectionAsync<TResult>(
        Func<SqlConnection, Task<TResult>> operation, bool readOnly,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var connection = await connectionFactory.OpenAsync(cancellationToken);
                return await operation(connection);
            }
            catch (SqlException error) when (readOnly && error.Number == 1205 && attempt < 2)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(125 * (attempt + 1)), cancellationToken);
            }
            catch (SqlException error)
            {
                throw DatabaseErrorMapper.Map(error);
            }
        }
    }
}

/// <summary>
/// 
/// </summary>
internal static class DatabaseErrorMapper
{
    // شماره خطاهایی که در زمان همزمانی برگشت داده می شود.
    private static readonly HashSet<int> ConcurrencyNumbers =
    [
        52005, 52006, 52008, 52010, 52105, 52106, 52108, 52110,
        52210, 52211, 52212, 52213, 52304, 52305
    ];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    public static DatabaseFailureException Map(SqlException error)
    {
        var kind = error.Number switch
        {
            2601 or 2627 => DatabaseFailureKind.Duplicate,
            1205 or -2 => DatabaseFailureKind.Transient,
            _ when ConcurrencyNumbers.Contains(error.Number) => DatabaseFailureKind.Concurrency,
            >= 52000 and <= 52999 => DatabaseFailureKind.Business,
            _ => DatabaseFailureKind.Unexpected
        };

        var message = kind is DatabaseFailureKind.Unexpected or DatabaseFailureKind.Transient
            ? "خطا در ارتباط یا اجرای عملیات پایگاه داده رخ داد."
            : error.Message;

        return new DatabaseFailureException(message, error.Number, kind, error);
    }
}

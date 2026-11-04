using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using TIR.Application.Common;
using TIR.Application.Contracts;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Bulk;

internal sealed class BulkWizardRepository(
    StoredProcedureExecutor executor, ILogger<BulkWizardRepository> logger) : IBulkWizardRepository
{
    // هر بخش ده هزار تایی هنگفایر در تراکنش های کوتاه تر دیتابیس اجرا می شود.
    private const int SqlTransactionRows = 1000;
    private const int FormulaSqlRows = 500;
    private const int DeadlockRetryCount = 5;
    private const int FormulaTransientRetryCount = 3;

    public Task<Guid> StageAsync(byte operationKind, long? formulaId,
        IReadOnlyList<BulkWizardEntry> rows, CancellationToken cancellationToken) =>
        executor.WithConnectionAsync(async connection =>
        {
            if (rows.Count == 0) throw new ArgumentException("درخواست خالی است.");
            var workId = Guid.NewGuid();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await connection.ExecuteAsync(StoredProcedureExecutor.Command(
                    "dbo.SP_BulkWork_Create", new { WorkId = workId, OperationKind = operationKind,
                        FormulaId = formulaId, TotalCount = rows.Count }, cancellationToken,
                    transaction: transaction));

                var employees = CreateItemsTable();
                var values = CreateValuesTable();
                foreach (var row in rows)
                {
                    employees.Rows.Add(workId, row.InputOrdinal, Db(row.ExcelRow),
                        Db(row.EmployeeId), Db(row.EmployeeCode), Db(row.FirstName),
                        Db(row.LastName), Db(row.ExpectedEmployeeRowVersion));

                    if (employees.Rows.Count >= 3000)
                        await CopyAsync(connection, transaction, "dbo.BulkWorkItem", employees, cancellationToken);
                }
                await CopyAsync(connection, transaction, "dbo.BulkWorkItem", employees, cancellationToken);

                foreach (var row in rows)
                {
                    foreach (var value in row.Values)
                    {
                        values.Rows.Add(workId, row.InputOrdinal, Db(value.AttributeId),
                            Db(value.OperationKind), Db(value.StringValue), Db(value.IntegerValue),
                            Db(value.DecimalValue), Db(value.DateValue), Db(value.TimeValue),
                            Db(value.DateTimeUtc), Db(value.BooleanValue), Db(value.OptionId),
                            Db(value.ExpectedRowVersion));
                        if (values.Rows.Count >= 3000)
                            await CopyAsync(connection, transaction, "dbo.BulkWorkValue", values, cancellationToken);
                    }
                }
                await CopyAsync(connection, transaction, "dbo.BulkWorkValue", values, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return workId;
            }
            catch
            {
                if (transaction.Connection is not null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }, false, cancellationToken);

    public async Task ProcessChunkAsync(Guid workId, int chunkNumber, int chunkSize,
        CancellationToken cancellationToken)
    {
        if (chunkNumber < 0 || chunkSize < 1 || chunkSize % SqlTransactionRows != 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize),
                "اندازه بخش باید مضربی از هزار باشد.");

        var firstSqlChunk = checked(chunkNumber * (chunkSize / SqlTransactionRows));
        for (var offset = 0; offset < chunkSize / SqlTransactionRows; offset++)
        {
            var sqlChunk = checked(firstSqlChunk + offset);
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await executor.ExecuteAsync("dbo.SP_BulkWork_ProcessChunk", new
                    {
                        WorkId = workId,
                        ChunkNumber = sqlChunk,
                        ChunkSize = SqlTransactionRows
                    }, cancellationToken, timeoutSeconds: 180);
                    break;
                }
                catch (DatabaseFailureException error)
                    when ((error.ErrorNumber is 1205 or 52816) && attempt < DeadlockRetryCount)
                {
                    // فقط تراکنشی تکرار می شود که SQL در بن بست آن را برگردانده است.
                    // بخش های تکمیل شده به کمک وضعیت هر ردیف دوباره اجرا نمی شوند.
                    var delay = TimeSpan.FromMilliseconds(200 * (1 << attempt) + Random.Shared.Next(50, 250));
                    logger.LogWarning(error,
                        "بن بست موقت در درخواست {WorkId}، بخش دیتابیس {SqlChunk}، تلاش {Attempt}",
                        workId, sqlChunk, attempt + 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
    }

    public async Task<(Guid WorkId, int TotalCount)> StageFormulaAllAsync(long formulaId,
        CancellationToken cancellationToken)
    {
        var workId = Guid.NewGuid();
        var total = await executor.SingleAsync<int>("dbo.SP_BulkWork_CreateFormulaAll",
            new { WorkId = workId, FormulaId = formulaId }, cancellationToken);
        return (workId, total);
    }

    public async Task ProcessFormulaChunkAsync(Guid workId, int chunkNumber, int chunkSize,
        short utcOffsetMinutes, CancellationToken cancellationToken)
    {
        if (chunkNumber < 0 || chunkSize < 1 || chunkSize % FormulaSqlRows != 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize),
                "اندازه بخش فرمول باید مضربی از پانصد باشد.");

        // Job ده هزار نفری حفظ می شود اما هر محاسبه با تراکنش کوتاه تری انجام می شود.
        var firstSqlChunk = checked(chunkNumber * (chunkSize / FormulaSqlRows));
        for (var offset = 0; offset < chunkSize / FormulaSqlRows; offset++)
        {
            var sqlChunk = checked(firstSqlChunk + offset);
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await executor.ExecuteAsync("dbo.SP_BulkWork_ProcessFormulaChunk", new
                    {
                        WorkId = workId,
                        ChunkNumber = sqlChunk,
                        ChunkSize = FormulaSqlRows,
                        UtcOffsetMinutes = utcOffsetMinutes
                    }, cancellationToken, timeoutSeconds: 240);
                    break;
                }
                catch (DatabaseFailureException error)
                    when ((error.ErrorNumber is 1205 or -2) && attempt < FormulaTransientRetryCount)
                {
                    // تکرار از همان زیرگروه است؛ وضعیت هر کارمند از اجرای دوباره جلوگیری می کند.
                    var delay = TimeSpan.FromMilliseconds(500 * (1 << attempt) + Random.Shared.Next(100, 450));
                    logger.LogWarning(error,
                        "خطای موقت محاسبه فرمول {WorkId}، زیرگروه {SqlChunk}، تلاش {Attempt}",
                        workId, sqlChunk, attempt + 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }
    }

    public async Task<(byte OperationKind, int TotalCount)> QueueAsync(Guid workId,
        CancellationToken cancellationToken)
    {
        var row = await executor.SingleAsync<BulkWizardStatusDto>("dbo.SP_BulkWork_Queue",
            new { WorkId = workId }, cancellationToken);
        return (row.OperationKind, row.TotalCount);
    }

    public Task<BulkWizardReport?> ReportAsync(Guid workId, int errorPage, CancellationToken cancellationToken) =>
        executor.WithConnectionAsync(async connection =>
        {
            using var result = await connection.QueryMultipleAsync(StoredProcedureExecutor.Command(
                "dbo.SP_BulkWork_Report", new { WorkId = workId, ErrorPage = errorPage,
                    ErrorPageSize = 100 }, cancellationToken));
            var header = await result.ReadSingleOrDefaultAsync<BulkWizardStatusDto>();
            var errors = (await result.ReadAsync<BulkWizardErrorDto>()).ToList();
            return header is null ? null : new BulkWizardReport(header, errors);
        }, true, cancellationToken);

    public Task<int> CleanupAsync(DateTime olderThanUtc, CancellationToken cancellationToken) =>
        executor.SingleAsync<int>("dbo.SP_BulkWork_Cleanup",
            new { OlderThanUtc = olderThanUtc, MaxRequests = 100 }, cancellationToken);

    private static DataTable CreateItemsTable()
    {
        var table = new DataTable();
        table.Columns.Add("WorkId", typeof(Guid));
        table.Columns.Add("InputOrdinal", typeof(int));
        table.Columns.Add("ExcelRow", typeof(int));
        table.Columns.Add("EmployeeId", typeof(long));
        table.Columns.Add("EmployeeCode", typeof(string));
        table.Columns.Add("FirstName", typeof(string));
        table.Columns.Add("LastName", typeof(string));
        table.Columns.Add("ExpectedEmployeeRowVersion", typeof(byte[]));
        return table;
    }

    private static DataTable CreateValuesTable()
    {
        var table = new DataTable();
        table.Columns.Add("WorkId", typeof(Guid));
        table.Columns.Add("InputOrdinal", typeof(int));
        table.Columns.Add("AttributeId", typeof(long));
        table.Columns.Add("OperationKind", typeof(byte));
        table.Columns.Add("StringValue", typeof(string));
        table.Columns.Add("IntegerValue", typeof(long));
        table.Columns.Add("DecimalValue", typeof(decimal));
        table.Columns.Add("DateValue", typeof(DateTime));
        table.Columns.Add("TimeValue", typeof(TimeSpan));
        table.Columns.Add("DateTimeUtc", typeof(DateTime));
        table.Columns.Add("BooleanValue", typeof(bool));
        table.Columns.Add("OptionId", typeof(long));
        table.Columns.Add("ExpectedRowVersion", typeof(byte[]));
        return table;
    }

    private static async Task CopyAsync(SqlConnection connection, SqlTransaction transaction,
        string target, DataTable source, CancellationToken cancellationToken)
    {
        if (source.Rows.Count == 0) return;
        using var copy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = target,
            BatchSize = source.Rows.Count,
            BulkCopyTimeout = 600
        };
        foreach (DataColumn column in source.Columns)
            copy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        await copy.WriteToServerAsync(source, cancellationToken);
        source.Clear();
    }

    private static object Db(object? value) => value ?? DBNull.Value;
}

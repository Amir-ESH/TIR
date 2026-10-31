using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using TIR.Application.Contracts;
using TIR.Infrastructure.Data;

namespace TIR.Infrastructure.Bulk;

internal sealed class BulkRepository(StoredProcedureExecutor executor) : IBulkRepository
{
    public Task<Guid> StageAsync(IAsyncEnumerable<BulkEmployeeInput> employees,
        BulkRequestOptions options, CancellationToken cancellationToken) =>
        executor.WithConnectionAsync(async connection =>
        {
            // ساخت Batch و بارگذاری تمام ورودی ها اتمیک است. بارگذاری ناقص ثبت نمی شود.
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("IdempotencyKey", options.IdempotencyKey);
            parameters.Add("BatchId", dbType: DbType.Guid, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(StoredProcedureExecutor.Command(
                "dbo.SP_Batch_Create", parameters, cancellationToken, transaction: transaction));
            var batchId = parameters.Get<Guid>("BatchId");

            var employeeRows = CreateEmployeeTable();
            var valueRows = CreateValueTable();
            var inputOrdinal = 0;

            await foreach (var input in employees.WithCancellation(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (input is null || input.Values is null)
                    throw new ArgumentException("ورودی کارمند و فهرست مقادیر نمی تواند NULL باشد.");

                if (inputOrdinal == int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(employees), "تعداد ورودی ها بیش از ظرفیت است.");

                inputOrdinal++;

                employeeRows.Rows.Add(batchId, inputOrdinal, Db(input.EmployeeId));

                var lineOrdinal = 0;
                foreach (var value in input.Values)
                {
                    if (value is null)
                        throw new ArgumentException("یک ردیف مقدار NULL است.");

                    lineOrdinal++;

                    if (value.ExpectedRowVersion is { Length: not 8 })
                        throw new ArgumentException("نسخه مقدار باید هشت بایت باشد.");

                    valueRows.Rows.Add(batchId, inputOrdinal, lineOrdinal,
                        Db(value.AttributeId), Db(value.OperationKind), Db(value.StringValue),
                        Db(value.IntegerValue), Db(value.DecimalValue), Db(value.DateValue?.Date),
                        Db(value.TimeValue), Db(value.DateTimeUtc), Db(value.BooleanValue),
                        Db(value.OptionId), Db(value.ExpectedRowVersion));

                    if (valueRows.Rows.Count >= options.TransferBatchSize)
                        await CopyAsync(connection, transaction, "dbo.ValueInput", valueRows, cancellationToken);
                }

                if (employeeRows.Rows.Count >= options.TransferBatchSize)
                    await CopyAsync(connection, transaction, "dbo.EmployeeInput", employeeRows, cancellationToken);
            }

            if (inputOrdinal == 0)
                throw new ArgumentException("درخواست گروهی هیچ کارمندی ندارد.", nameof(employees));

            await CopyAsync(connection, transaction, "dbo.EmployeeInput", employeeRows, cancellationToken);
            await CopyAsync(connection, transaction, "dbo.ValueInput", valueRows, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return batchId;
        }, false, cancellationToken);

    public Task<BulkReportDto> ProcessAsync(Guid batchId, int employeesPerTransaction, CancellationToken cancellationToken)
        => executor.WithConnectionAsync(async connection =>
        {
            // پردازش تنها یک بار برای هر Batch اجرا می شود. در خطای مبهم تکرار خودکار نداریم.
            using var results = await connection.QueryMultipleAsync(StoredProcedureExecutor.Command(
                "dbo.SP_Batch_Process", new { BatchId = batchId, EmployeesPerTransaction = employeesPerTransaction },
                cancellationToken, timeoutSeconds: 1200));
            return await ReadReportAsync(results);
        }, false, cancellationToken);

    public Task<BulkReportDto?> GetReportAsync(Guid batchId, CancellationToken cancellationToken) =>
        executor.WithConnectionAsync(async connection =>
        {
            using var results = await connection.QueryMultipleAsync(StoredProcedureExecutor.Command(
                "dbo.SP_Batch_GetReport", new { BatchId = batchId }, cancellationToken, timeoutSeconds: 180));
            return await ReadOptionalReportAsync(results);
        }, true, cancellationToken);

    public Task DeleteAsync(Guid batchId, CancellationToken cancellationToken) =>
        executor.ExecuteAsync("dbo.SP_Batch_Delete", new { BatchId = batchId }, cancellationToken);

    public Task<int> CleanupAsync(DateTime olderThanUtc, int maxBatches,
        CancellationToken cancellationToken) =>
        executor.SingleAsync<int>("dbo.SP_Batch_Cleanup",
            new { OlderThanUtc = olderThanUtc, MaxBatches = maxBatches }, cancellationToken,
            timeoutSeconds: 600);

    private static async Task<BulkReportDto> ReadReportAsync(SqlMapper.GridReader results) =>
        await ReadOptionalReportAsync(results)
            ?? throw new InvalidOperationException("گزارش پردازش گروهی توسط SQL برگردانده نشد.");

    private static async Task<BulkReportDto?> ReadOptionalReportAsync(SqlMapper.GridReader results)
    {
        var header = await results.ReadSingleOrDefaultAsync<BulkReportHeaderDto>();
        var employees = (await results.ReadAsync<BulkEmployeeResultDto>()).ToList();
        var errors = (await results.ReadAsync<BulkErrorDto>()).ToList();
        return header is null ? null : new BulkReportDto(header, employees, errors);
    }

    private static DataTable CreateEmployeeTable()
    {
        var table = new DataTable();
        table.Columns.Add("BatchId", typeof(Guid));
        table.Columns.Add("InputOrdinal", typeof(int));
        table.Columns.Add("EmployeeId", typeof(long));
        return table;
    }

    private static DataTable CreateValueTable()
    {
        var table = new DataTable();
        table.Columns.Add("BatchId", typeof(Guid));
        table.Columns.Add("InputOrdinal", typeof(int));
        table.Columns.Add("LineOrdinal", typeof(int));
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
        string tableName, DataTable rows, CancellationToken cancellationToken)
    {
        if (rows.Rows.Count == 0) return;

        using var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
        {
            DestinationTableName = tableName,
            BatchSize = rows.Rows.Count,
            BulkCopyTimeout = 600
        };

        // نگاشت نامی از درج داده در ستون Id و RowVersion جلوگیری می کند.
        foreach (DataColumn column in rows.Columns)
            bulk.ColumnMappings.Add(column.ColumnName, column.ColumnName);

        await bulk.WriteToServerAsync(rows, cancellationToken);
        rows.Clear();
    }

    private static object Db(object? value) => value ?? DBNull.Value;
}

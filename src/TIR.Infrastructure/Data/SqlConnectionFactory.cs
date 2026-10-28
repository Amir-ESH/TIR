using Microsoft.Data.SqlClient;

namespace TIR.Infrastructure.Data;

/// <summary>
/// ایجاد اتصال مستقل برای هر عملیات ورودی و خروجی.
/// </summary>
internal sealed class SqlConnectionFactory(string connectionString)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}

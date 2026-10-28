using Hangfire;
using Hangfire.SqlServer;
using Serilog;
using TIR.Application;
using TIR.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((_, _, logging) => logging
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(
        builder.Configuration.GetConnectionString("Test") ?? string.Empty);

    // فعال شدن Hangfire به آماده بودن دیتابیس نگهداری Job ها وابسته است.
    if (builder.Configuration.GetValue<bool>("Hangfire:Enabled"))
    {
        var hangfireConnection = builder.Configuration.GetConnectionString("Hangfire");
        if (string.IsNullOrWhiteSpace(hangfireConnection))
            throw new InvalidOperationException("رشته اتصال Hangfire تنظیم نشده است.");

        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(hangfireConnection, new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = false
            }));
        builder.Services.AddHangfireServer();
    }

    var app = builder.Build();
    app.UseSerilogRequestLogging();

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "خطا هنگام راه اندازی برنامه TIR");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
